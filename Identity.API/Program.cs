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
using Identity.Infrastructure;
using Identity.Middleware;
using Identity.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
  .AddJsonFile(builder.Environment.IsEnvironment("Test") ? "./test.appsettings.json" : "/config/appsettings.json");

#region services
// Add services to the container.
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
      options.ClaimsIdentity.EmailClaimType = OpenIddictConstants.Claims.Email;
      options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
      options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
    })
    .AddEntityFrameworkStores<ApplicationDBContext>()
    .AddDefaultTokenProviders();
builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
       o.TokenLifespan = TimeSpan.FromMinutes(int.Parse(builder.Configuration["OpenId:TokenLifespanMinutes"])));
#endregion

#region authentication
builder.Services.AddQuartz(options =>
{
  options.SchedulerName = $"QuartzScheduler-{Guid.NewGuid()}";
  options.UseMicrosoftDependencyInjectionJobFactory();
  options.UseSimpleTypeLoader();
  options.UseInMemoryStore();
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
builder.Services.AddAuthentication(options =>
{
  options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});
builder.Services.AddAuthorization(option =>
{
  option.AddPolicy(Identity.Application.IdentityConstants.Roles.Admin, policy =>
  {
    policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
    policy.RequireAuthenticatedUser();
    policy.RequireClaim(OpenIddictConstants.Claims.Role, Identity.Application.IdentityConstants.Roles.Admin);
  });
});
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
      options.UseEntityFrameworkCore().UseDbContext<ApplicationDBContext>().ReplaceDefaultEntities<OpenIddictClientApplication, OpenIddictClientAuthorization, OpenIddictClientScope, OpenIddictClientToken, string>();
      options.UseQuartz(options =>
      {
        options.SetMinimumTokenLifespan(TimeSpan.FromDays(int.Parse(builder.Configuration["OpenId:CleanOrphanTokenDays"])));
        options.SetMinimumAuthorizationLifespan(TimeSpan.FromDays(int.Parse(builder.Configuration["OpenId:CleanOrphanTokenDays"])));
      });
    })
    .AddServer(options =>
    {
      options.AllowPasswordFlow();
      options.AllowRefreshTokenFlow();
      options.AllowClientCredentialsFlow();

      options.SetTokenEndpointUris("/api/v1.0/identity/connect/token")
             .SetUserinfoEndpointUris("/api/v1.0/identity/connect/userinfo")
             .SetLogoutEndpointUris("/api/v1.0/identity/connect/logout");

      options.UseReferenceAccessTokens();
      options.UseReferenceRefreshTokens();

      options.SetAccessTokenLifetime(TimeSpan.FromMinutes(int.Parse(builder.Configuration["OpenId:AccessTokenLifetimeMinutes"])));
      options.SetRefreshTokenLifetime(TimeSpan.FromMinutes(int.Parse(builder.Configuration["OpenId:RefreshTokenLifetimeMinutes"])));
      options.SetRefreshTokenReuseLeeway(TimeSpan.FromSeconds(int.Parse(builder.Configuration["OpenId:RefreshTokenReuseLeewaySeconds"])));

      options.RegisterScopes(Identity.Application.IdentityConstants.Scopes.Upload);

      if (builder.Environment.IsEnvironment("Test"))
      {
        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey();
      }
      else
      {
        options.AddSigningCertificate(new System.Security.Cryptography.X509Certificates.X509Certificate2(builder.Configuration["OpenId:SigningCert"]))
               .AddEncryptionCertificate(new System.Security.Cryptography.X509Certificates.X509Certificate2(builder.Configuration["OpenId:EncryptionCert"]));
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

  if (!builder.Environment.IsEnvironment("Test"))
  {
    Func<ApplicationDBContext> factory = () =>
    {
      var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
      optionsBuilder.UseNpgsql(builder.Configuration.GetConnectionString("IdentityConnection"));
      optionsBuilder.UseOpenIddict<OpenIddictClientApplication, OpenIddictClientAuthorization, OpenIddictClientScope, OpenIddictClientToken, string>();

      return new ApplicationDBContext(optionsBuilder.Options);
    };

    cBuilder.RegisterInstance(factory);
  }

  // automapper
  cBuilder.Register(context => new MapperConfiguration(cfg =>
  {
    cfg.CreateMap<ClientUpdateRequest, Client>();
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
    var cert = builder.Configuration["TLS:Cert"];
    var pwd = builder.Configuration["TLS:Password"];
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

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
  endpoints.MapControllers();
  endpoints.MapHub<RegisterClientHub>("/hub/client");
});
#endregion

app.Run();

public partial class Program { }
