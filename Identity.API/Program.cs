using Asp.Versioning;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Identity.Application.Services;
using Identity.DBContext;
using Identity.DBContext.Models;
using Identity.Filters;
using Identity.Handlers;
using Identity.Infrastructure;
using Identity.Middleware;
using Identity.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Quartz;
using Rebus.Config;
using Rebus.Transport.InMem;
using ScottBrady91.AspNetCore.Identity;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

#pragma warning disable CA2208 // Instantiate argument exceptions correctly
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
  .AddJsonFile(builder.Environment.IsEnvironment("Test") ? "./test.appsettings.json" : "/configs/appsettings.json");

#region services
// Add services to the container.
builder.Services.AddScoped<GroupExistsFilter>();
builder.Services.AddScoped<UserExistsFilter>();
builder.Services.AddScoped<TokenRegistrationFilter>();
builder.Services.AddSignalR().AddJsonProtocol();
builder.Services.AddControllers();
if (!builder.Environment.IsEnvironment("Test"))
{
  builder.Services.AddStackExchangeRedisCache(options =>
  {
    options.Configuration = builder.Configuration.GetConnectionString("DistributedCache");
    options.InstanceName = "wekode.mml.cache";
  });
}
builder.Services.AddApiVersioning(config =>
{
  config.DefaultApiVersion = new ApiVersion(1, 0);
  config.AssumeDefaultVersionWhenUnspecified = true;
});
builder.Services.AddEndpointsApiExplorer();
if (builder.Environment.IsDevelopment())
{
  // configuring Swagger/OpenAPI. More at https://aka.ms/aspnetcore/swashbuckle
  builder.Services.AddSwaggerGen(config =>
  {
    config.SwaggerDoc("v1.0", new OpenApiInfo { Title = "Identity Api", Version = "v1.0" });
    config.OperationFilter<RemoveVersionParameterFilter>();
    config.DocumentFilter<ReplaceVersionWithExactValueInPathFilter>();
    config.EnableAnnotations();
  });
}
builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(builder =>
  {
    builder.AllowAnyOrigin()
             .AllowAnyMethod()
             .AllowAnyHeader();
  });
});

if (!builder.Environment.IsEnvironment("Test"))
{
  // Configure Rebus with RabbitMQ transport
  var mBusHost = builder.Configuration["MessageBus:Host"] ?? throw new ArgumentNullException("MessageBus:Host");
  var mBusVirtualHost = builder.Configuration["MessageBus:VirtualHost"];
  var mBusUser = builder.Configuration["MessageBus:User"] ?? throw new ArgumentNullException("MessageBus:User");
  var mBusPassword = builder.Configuration["MessageBus:Password"] ?? throw new ArgumentNullException("MessageBus:Password");

  var mBusConnection = $"amqp://{mBusUser}:{mBusPassword}@{mBusHost}";
  if (!string.IsNullOrEmpty(mBusVirtualHost))
  {
    mBusConnection += $"/{mBusVirtualHost}";
  }

  builder.Services.AddRebus(configure =>
      configure.Transport(t => t.UseRabbitMq(mBusConnection, "mml-queue"))
  );
}
else
{
  // Use Rebus in-memory transport for tests to avoid external RabbitMQ dependency
  var inMemNetwork = new InMemNetwork();
  builder.Services.AddSingleton(inMemNetwork);
  builder.Services.AddRebus(config =>
    config.Transport(t => t.UseInMemoryTransport(inMemNetwork, "identity-test-queue"))
  );
}
#endregion

#region localizations
builder.Services.AddMvc().AddDataAnnotationsLocalization(options =>
{
  options.DataAnnotationLocalizerProvider = (type, factory) =>
      factory.Create(typeof(Identity.Resources.ValidationMessages));
});
#endregion

#region dbContext
builder.Services.AddDbContext<ApplicationDBContext>(options =>
{
  options.UseNpgsql(builder.Configuration.GetConnectionString("IdentityConnection"));
  options.UseOpenIddict<OpenIddictClientApplication, OpenIddictClientAuthorization, OpenIddictClientScope, OpenIddictClientToken, string>();
});
builder.Services.AddIdentity<IdentityUser<long>, IdentityRole<long>>(options =>
    {
      options.Password.RequiredLength = 12;
      options.Password.RequireNonAlphanumeric = false;
      options.Password.RequireDigit = false;
      options.Password.RequireLowercase = false;
      options.Password.RequireUppercase = false;
      options.ClaimsIdentity.EmailClaimType = Claims.Email;
      options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
      options.ClaimsIdentity.RoleClaimType = Claims.Role;
    })
    .AddEntityFrameworkStores<ApplicationDBContext>()
    .AddDefaultTokenProviders();
builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
       o.TokenLifespan = TimeSpan.FromMinutes(int.Parse(builder.Configuration["OpenId:TokenLifespanMinutes"] ?? "60")));
#endregion

#region authentication
builder.Services.AddQuartz(options =>
{
  options.SchedulerName = $"QuartzScheduler-{Guid.NewGuid()}";
  options.UseSimpleTypeLoader();
  options.UseInMemoryStore();
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
builder.Services.AddAuthentication(options =>
{
  options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});
builder.Services.AddAuthorizationBuilder()
  .AddPolicy(Identity.Application.IdentityConstants.Roles.Admin, policy =>
  {
    policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
    policy.RequireAuthenticatedUser();
    policy.RequireClaim(OpenIddictConstants.Claims.Role, Identity.Application.IdentityConstants.Roles.Admin);
  })
  .AddPolicy(Identity.Application.IdentityConstants.Roles.Client, policy =>
  {
    policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
    policy.RequireAuthenticatedUser();
    policy.RequireClaim(OpenIddictConstants.Claims.Role, Identity.Application.IdentityConstants.Roles.Client);
  });
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
      options.UseEntityFrameworkCore().UseDbContext<ApplicationDBContext>().ReplaceDefaultEntities<OpenIddictClientApplication, OpenIddictClientAuthorization, OpenIddictClientScope, OpenIddictClientToken, string>();
      options.UseQuartz(options =>
      {
        options.SetMinimumTokenLifespan(TimeSpan.FromDays(int.Parse(builder.Configuration["OpenId:CleanOrphanTokenDays"] ?? "1")));
        options.SetMinimumAuthorizationLifespan(TimeSpan.FromDays(int.Parse(builder.Configuration["OpenId:CleanOrphanTokenDays"] ?? "1")));
      });
    })
    .AddServer(options =>
    {
      options.AllowPasswordFlow();
      options.AllowRefreshTokenFlow();
      options.AllowClientCredentialsFlow();

      options.SetIssuer(new Uri(builder.Configuration["OpenId:Issuer"] ?? throw new ArgumentNullException("OpenId:Issuer")));
      options.SetTokenEndpointUris("api/v1.0/identity/connect/token")
             .SetUserInfoEndpointUris("api/v1.0/identity/connect/userinfo")
             .SetEndSessionEndpointUris("api/v1.0/identity/connect/logout")
             .SetIntrospectionEndpointUris("api/v1.0/identity/connect/introspect");

      options.UseReferenceAccessTokens();
      options.UseReferenceRefreshTokens();

      options.AddEventHandler<ApplyTokenResponseContext>(builder =>
            builder.UseSingletonHandler<ApplyTokenResponseHandler>());

      options.SetAccessTokenLifetime(TimeSpan.FromMinutes(int.Parse(builder.Configuration["OpenId:AccessTokenLifetimeMinutes"] ?? "60")));
      options.SetRefreshTokenLifetime(TimeSpan.FromMinutes(int.Parse(builder.Configuration["OpenId:RefreshTokenLifetimeMinutes"] ?? "15")));
      options.SetRefreshTokenReuseLeeway(TimeSpan.FromSeconds(int.Parse(builder.Configuration["OpenId:RefreshTokenReuseLeewaySeconds"] ?? "60")));

      if (builder.Environment.IsEnvironment("Test"))
      {
        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey();
      }
      else
      {
        var signingCert = X509CertificateLoader.LoadPkcs12(File.ReadAllBytes(builder.Configuration["OpenId:SigningCert"] ?? throw new ArgumentNullException("OpenId:SigningCert")), null);
        var encryptCert = X509CertificateLoader.LoadPkcs12(File.ReadAllBytes(builder.Configuration["OpenId:EncryptionCert"] ?? throw new ArgumentNullException("OpenId:EncryptionCert")), null);
        options.AddSigningCertificate(signingCert)
               .AddEncryptionCertificate(encryptCert);
      }

      var openidBuilder = options.UseAspNetCore()
             .EnableTokenEndpointPassthrough()
             .EnableUserInfoEndpointPassthrough()
             .EnableEndSessionEndpointPassthrough();

      if (builder.Environment.IsEnvironment("Test"))
      {
        openidBuilder.DisableTransportSecurityRequirement();
      }
    })
    .AddValidation(options =>
    {
      options.UseLocalServer();
      options.UseAspNetCore();
    });
#endregion

#region dependency injection
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(cBuilder =>
{
  cBuilder.RegisterType<ApplicationService>();
  cBuilder.RegisterType<ClientApplicationService>();
  if (!builder.Environment.IsEnvironment("Test"))
  {
    cBuilder.RegisterType<BCryptPasswordHasher<IdentityUser<long>>>().AsImplementedInterfaces();
  }

  cBuilder.RegisterType<SqlIdentityRepository>().AsImplementedInterfaces();
  cBuilder.RegisterType<SqlClientRepository>().AsImplementedInterfaces();
  cBuilder.RegisterType<SqlGroupRepository>().AsImplementedInterfaces();

  if (!builder.Environment.IsEnvironment("Test"))
  {
    ApplicationDBContext factory()
    {
      var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
      optionsBuilder.UseNpgsql(builder.Configuration.GetConnectionString("IdentityConnection"));
      optionsBuilder.UseOpenIddict<OpenIddictClientApplication, OpenIddictClientAuthorization, OpenIddictClientScope, OpenIddictClientToken, string>();

      return new ApplicationDBContext(optionsBuilder.Options);
    }

    cBuilder.RegisterInstance(factory);
  }
});
#endregion

#region kestrel
builder.WebHost.ConfigureKestrel(options =>
{
  options.ListenAnyIP(5051, listenOptions =>
  {
    var cert = builder.Configuration["TLS:Cert"] ?? throw new ArgumentNullException("TLS:Cert");
    var pwd = builder.Configuration["TLS:Password"] ?? throw new ArgumentNullException("TLS:Password");
    listenOptions.UseHttps(cert, pwd);
  });
});
#endregion

var app = builder.Build();

#region db migrations
using var serviceScope = app.Services.CreateScope();
var db = serviceScope.ServiceProvider.GetRequiredService<ApplicationDBContext>().Database;
if (db.IsRelational())
{
  db.Migrate();
}
var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<long>>>();
if (!roleManager.RoleExistsAsync(Identity.Application.IdentityConstants.Roles.Admin).Result)
{
  roleManager.CreateAsync(new IdentityRole<long>(Identity.Application.IdentityConstants.Roles.Admin)).GetAwaiter().GetResult();
}
#endregion

#region middleware configuration
app.UseApiKeyValidation();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI(config =>
  {
    config.SwaggerEndpoint("/swagger/v1.0/swagger.json", "Identity API v1.0");
  });
  app.UseDeveloperExceptionPage();
}

var supportedCultures = new[] { "en", "en_US", "de", "de_DE", "ru", "ru_RU" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<RegisterClientHub>("/hub/client");
#endregion

// Create api clients if not exist
using var scope = app.Services.CreateScope();
var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
var apiClientsSection = app.Configuration.GetSection("ApiClients");
foreach (IConfigurationSection apiClient in apiClientsSection.GetChildren())
{
  var id = apiClient.GetValue<string>("ClientId");
  var secret = apiClient.GetValue<string>("ClientSecret");
  if (await manager.FindByClientIdAsync(id!) is null)
  {
    await manager.CreateAsync(new OpenIddictApplicationDescriptor
    {
      ClientId = id,
      ClientSecret = secret,
      Permissions =
      {
        Permissions.Endpoints.Introspection
      }
    });
  }
}

app.Run();
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
