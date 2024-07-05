using Asp.Versioning;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using Identity.Application;
using Identity.Application.Models;
using Identity.Application.Services;
using Identity.Contracts;
using Identity.DBContext;
using Identity.DBContext.Models;
using Identity.Filters;
using Identity.Handlers;
using Identity.Infrastructure;
using Identity.Middleware;
using Identity.Sockets;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Quartz;
using ScottBrady91.AspNetCore.Identity;
using System;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;
using DbGroup = Identity.DBContext.Models.Group;
using Group = Identity.Application.Models.Group;

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
  builder.Services.AddMassTransit(mt =>
  {
    mt.UsingRabbitMq((context, cfg) =>
    {
      cfg.Host(builder.Configuration["MassTransit:Host"], builder.Configuration["MassTransit:VirtualHost"], h =>
      {

        h.Username(builder.Configuration["MassTransit:User"] ?? throw new ArgumentNullException("MassTransit:User"));
        h.Password(builder.Configuration["MassTransit:Password"] ?? throw new ArgumentNullException("MassTransit:Password"));
      });

      cfg.ConfigureEndpoints(context);
    });
  });
  builder.Services.AddOptions<MassTransitHostOptions>()
  .Configure(options =>
  {
    options.WaitUntilStarted = bool.Parse(builder.Configuration["MassTransit:WaitUntilStarted"] ?? "False");
    options.StartTimeout = TimeSpan.FromSeconds(double.Parse(builder.Configuration["MassTransit:StartTimeoutSeconds"] ?? "60"));
    options.StopTimeout = TimeSpan.FromSeconds(double.Parse(builder.Configuration["MassTransit:StopTimeoutSeconds"] ?? "60"));
  });
}
else
{
  builder.Services.AddMassTransitTestHarness(x =>
   {
     x.AddDelayedMessageScheduler();
     x.UsingInMemory((context, cfg) =>
     {
       cfg.UseDelayedMessageScheduler();
       cfg.ConfigureEndpoints(context);
     });
   });
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
             .SetUserinfoEndpointUris("api/v1.0/identity/connect/userinfo")
             .SetLogoutEndpointUris("api/v1.0/identity/connect/logout")
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
        options.AddSigningCertificate(new System.Security.Cryptography.X509Certificates.X509Certificate2(builder.Configuration["OpenId:SigningCert"] ?? throw new ArgumentNullException("OpenId:SigningCert")))
               .AddEncryptionCertificate(new System.Security.Cryptography.X509Certificates.X509Certificate2(builder.Configuration["OpenId:EncryptionCert"] ?? throw new ArgumentNullException("OpenId:EncryptionCert")));
      }

      var openidBuilder = options.UseAspNetCore()
             .EnableTokenEndpointPassthrough()
             .EnableUserinfoEndpointPassthrough()
             .EnableLogoutEndpointPassthrough();

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

  // automapper
  cBuilder.Register(context => new MapperConfiguration(cfg =>
  {
    cfg.CreateMap<ClientUpdateRequest, Client>();
    cfg.CreateMap<DbGroup, Group>();
    cfg.CreateMap<TagFilter, Identity.Application.Contracts.TagFilter>();
  })).AsSelf().SingleInstance();
  cBuilder.Register(c =>
  {
    //This resolves a new context that can be used later.
    var context = c.Resolve<IComponentContext>();
    var config = context.Resolve<MapperConfiguration>();
    return config.CreateMapper(context.Resolve);
  })
  .As<IMapper>()
  .InstancePerLifetimeScope();
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

public partial class Program { }
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
