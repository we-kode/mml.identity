using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using Identity.Application;
using Identity.DBContext;
using Identity.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using ScottBrady91.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

#pragma warning disable CA2208 // Instantiate argument exceptions correctly
namespace Identity.CLI
{
  public class Program
  {
    public static async Task Main(string[] args)
    {
      var configBuilder = new ConfigurationBuilder()
        .AddJsonFile("/configs/appsettings.json", true, true);
      var config = configBuilder.Build();

      await Host.CreateDefaultBuilder(args)
          .ConfigureAppConfiguration(c => configBuilder.Build())
          .UseServiceProviderFactory(new AutofacServiceProviderFactory())
          .ConfigureServices((hostContext, services) =>
          {
            services.AddLogging(cfg =>
            {
              cfg.ClearProviders();
            });
            services.AddHostedService<ConsoleHostedService>();
            services.AddDbContext<ApplicationDBContext>(options =>
            {
              options.UseNpgsql(config.GetConnectionString("IdentityConnection"));
            });
            services.AddIdentity<IdentityUser<long>, IdentityRole<long>>(options =>
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

            services.AddMassTransit(mt =>
            {
              mt.UsingRabbitMq((context, cfg) =>
              {
                cfg.Host(config["MassTransit:Host"], config["MassTransit:VirtualHost"], h =>
                {
                  h.Username(config["MassTransit:User"] ?? throw new ArgumentNullException("MassTransit:User"));
                  h.Password(config["MassTransit:Password"] ?? throw new ArgumentNullException("MassTransit:Password"));
                });

                cfg.ConfigureEndpoints(context);
              });
            });
            services.AddOptions<MassTransitHostOptions>()
              .Configure(options =>
              {
                options.WaitUntilStarted = bool.Parse(config["MassTransit:WaitUntilStarted"] ?? "False");
                options.StartTimeout = TimeSpan.FromSeconds(double.Parse(config["MassTransit:StartTimeoutSeconds"] ?? "60"));
                options.StopTimeout = TimeSpan.FromSeconds(double.Parse(config["MassTransit:StopTimeoutSeconds"] ?? "60"));
              });
          })
          .ConfigureContainer<ContainerBuilder>((context, cBuilder) =>
          {
            cBuilder.RegisterType<ApplicationService>();
            cBuilder.RegisterType<BCryptPasswordHasher<IdentityUser<long>>>().AsImplementedInterfaces();

            cBuilder.RegisterType<SqlGroupRepository>().AsImplementedInterfaces();
            cBuilder.RegisterType<SqlIdentityRepository>().AsImplementedInterfaces();
            cBuilder.RegisterType<SqlClientRepository>().AsImplementedInterfaces();

            ApplicationDBContext factory()
            {
              var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
              optionsBuilder.UseNpgsql(config.GetConnectionString("IdentityConnection"));
              return new ApplicationDBContext(optionsBuilder.Options);
            }

            cBuilder.RegisterInstance(factory);

            cBuilder.RegisterInstance(factory);

            cBuilder.Register(context => new MapperConfiguration(cfg =>
            {
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
          })
          .RunConsoleAsync()
          .ConfigureAwait(false);
    }
  }
}
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
