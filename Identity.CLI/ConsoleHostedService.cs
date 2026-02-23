using Identity.Application.Contracts;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Identity.CLI
{
  public class ConsoleHostedService(IHostApplicationLifetime appLifetime, IIdentityRepository identityRepository, IClientRepository clientRepository) : IHostedService
  {
    public Task StartAsync(CancellationToken cancellationToken)
    {
      appLifetime.ApplicationStarted.Register(() =>
        {
          Task.Run(async () =>
          {
            try
            {
              var args = Environment.GetCommandLineArgs();
              if (args.Length == 1)
              {
                var adminUser = new AdminUser(identityRepository, clientRepository);
                if (!await adminUser.CreateUser().ConfigureAwait(false))
                {
                  return;
                }
                await adminUser.CreateAdminApp().ConfigureAwait(false);
                return;
              }

              if (args.Length < 2)
              {
                Console.WriteLine("Unknown command. Please try again.");
                return;
              }

              var adminClient = new AdminClient(clientRepository);
              switch (args[1])
              {
                case "-ac":
                  await adminClient.CreateAdminAppClient().ConfigureAwait(false);
                  return;
                case "-al":
                  adminClient.ListAdminAppClients();
                  return;
                case "-ad":
                  if (args.Length == 3)
                  {
                    if (!Guid.TryParse(args[2], out Guid clientId))
                    {
                      Console.WriteLine("Invalid Client id.");
                      return;
                    }
                    adminClient.DeleteAdminAppClient(clientId);
                    return;
                  }
                  Console.WriteLine("Please enter client id.");
                  return;
                default:
                  Console.WriteLine("Unknown command. Please try again.");
                  break;
              }
            }
            catch (Exception ex)
            {
              Console.WriteLine($"Unhandled exception! {ex}");
            }
            finally
            {
              // Stop the application once the work is done
              appLifetime.StopApplication();
            }
          });
        });

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }
  }
}
