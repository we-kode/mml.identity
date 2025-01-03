﻿using Identity.DBContext;
using Identity.DBContext.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using System;
using System.Linq;

namespace IdentityService.Test
{
  public static class TestApplication
  {
    public static readonly string UserName = "test@user.test";
    public static readonly string Password = "secret123456";

    public static WebApplicationFactory<Program> Build()
    {
      var oAuthCLient = new OpenIddictApplicationDescriptor
      {
        ClientId = "testClient",
        DisplayName = "test",
      };
      oAuthCLient.Permissions.Add("ept:token");
      oAuthCLient.Permissions.Add("ept:logout");
      oAuthCLient.Permissions.Add("gt:password");
      oAuthCLient.Permissions.Add("gt:refresh_token");
      oAuthCLient.Permissions.Add("scp:offline_access");

      Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

      var memoryDBName = Guid.NewGuid().ToString();

      return new WebApplicationFactory<Program>()
          .WithWebHostBuilder(builder =>
          {
            builder.UseEnvironment("Test");

            builder.ConfigureServices(services =>
            {
              var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDBContext>));

              services.Remove(descriptor!);

              services.AddDbContext<ApplicationDBContext>(options =>
              {
                options.UseInMemoryDatabase(memoryDBName);
                options.UseOpenIddict<OpenIddictClientApplication, OpenIddictClientAuthorization, OpenIddictClientScope, OpenIddictClientToken, string>();
              });

              services.AddDistributedMemoryCache();
              services.AddSignalR();
              //seed first user
              var serviceProvider = services.BuildServiceProvider();
              using var scope = serviceProvider.CreateScope();
              var scopedServices = scope.ServiceProvider;
              var userManager = scopedServices.GetRequiredService<UserManager<IdentityUser<long>>>();
              var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole<long>>>();
              roleManager.CreateAsync(new IdentityRole<long>(Identity.Application.IdentityConstants.Roles.Admin));
              var seededUser = new IdentityUser<long>
              {
                UserName = UserName,
                NormalizedUserName = UserName,
                EmailConfirmed = true
              };
              var tu = userManager.CreateAsync(seededUser, Password).GetAwaiter().GetResult();
              var tr = userManager.AddToRoleAsync(seededUser, Identity.Application.IdentityConstants.Roles.Admin).GetAwaiter().GetResult();

              var manager = scopedServices.GetRequiredService<IOpenIddictApplicationManager>();
              var existingClientApp = manager.FindByClientIdAsync(oAuthCLient.ClientId!).GetAwaiter().GetResult();
              if (existingClientApp == null)
              {
                manager.CreateAsync(oAuthCLient).GetAwaiter().GetResult();
              }

              ApplicationDBContext factory()
              {
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
                optionsBuilder.UseInMemoryDatabase(memoryDBName);
                optionsBuilder.UseOpenIddict<OpenIddictClientApplication, OpenIddictClientAuthorization, OpenIddictClientScope, OpenIddictClientToken, string>();

                return new ApplicationDBContext(optionsBuilder.Options);
              }

              services.AddSingleton(provider => (Func<ApplicationDBContext>)factory);
            });
          });
    }
  }
}
