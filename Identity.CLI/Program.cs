using Autofac;
using Autofac.Extensions.DependencyInjection;
using Identity.Application;
using Identity.DBContext;
using Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Rebus.Config;
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

            // Configure Rebus with RabbitMQ transport
            var mBusHost = config["MessageBus:Host"] ?? throw new ArgumentNullException("MessageBus:Host");
            var mBusVirtualHost = config["MessageBus:VirtualHost"];
            var mBusUser = config["MessageBus:User"] ?? throw new ArgumentNullException("MessageBus:User");
            var mBusPassword = config["MessageBus:Password"] ?? throw new ArgumentNullException("MessageBus:Password");

            var mBusConnection = $"amqp://{mBusUser}:{mBusPassword}@{mBusHost}";
            if (!string.IsNullOrEmpty(mBusVirtualHost))
            {
              mBusConnection += $"/{mBusVirtualHost}";
            }

            services.AddRebus(configure =>
                configure.Transport(t => t.UseRabbitMq(mBusConnection, "identity-queue"))
            );
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
          })
          .RunConsoleAsync()
          .ConfigureAwait(false);
    }
  }
}
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
