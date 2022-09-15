using Autofac;
using Autofac.Extensions.DependencyInjection;
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
                cfg.Host(config["MassTransit:Host"], config["MassTransit:VirtualHost"], h => {
                  h.Username(config["MassTransit:User"]);
                  h.Password(config["MassTransit:Password"]);
                });

                cfg.ConfigureEndpoints(context);
              });
            });
            services.AddOptions<MassTransitHostOptions>()
              .Configure(options =>
              {
                options.WaitUntilStarted = bool.Parse(config["MassTransit:WaitUntilStarted"]);
                options.StartTimeout = TimeSpan.FromSeconds(double.Parse(config["MassTransit:StartTimeoutSeconds"]));
                options.StopTimeout = TimeSpan.FromSeconds(double.Parse(config["MassTransit:StopTimeoutSeconds"]));
              });
          })
          .ConfigureContainer<ContainerBuilder>((context, cBuilder) =>
          {
            cBuilder.RegisterType<ApplicationService>();
            cBuilder.RegisterType<BCryptPasswordHasher<IdentityUser<long>>>().AsImplementedInterfaces();

            cBuilder.RegisterType<SqlGroupRepository>().AsImplementedInterfaces();
            cBuilder.RegisterType<SqlIdentityRepository>().AsImplementedInterfaces();
            cBuilder.RegisterType<SqlClientRepository>().AsImplementedInterfaces();

            Func<ApplicationDBContext> factory = () =>
            {
              var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
              optionsBuilder.UseNpgsql(config.GetConnectionString("IdentityConnection"));
              return new ApplicationDBContext(optionsBuilder.Options);
            };

            cBuilder.RegisterInstance(factory);
          })
          .RunConsoleAsync()
          .ConfigureAwait(false);
    }
  }
}
