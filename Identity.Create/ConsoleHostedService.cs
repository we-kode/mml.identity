using Identity.Application.Contracts;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Identity.Create
{
  public class ConsoleHostedService : IHostedService
  {
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IIdentityRepository _identityRepository;
    private readonly IClientRepository _clientRepository;

    public ConsoleHostedService(IHostApplicationLifetime appLifetime, IIdentityRepository identityRepository, IClientRepository clientRepository)
    {
      _appLifetime = appLifetime;
      _identityRepository = identityRepository;
      _clientRepository = clientRepository;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _ = _appLifetime.ApplicationStarted.Register(() =>
        {
          Task.Run(async () =>
          {
            try
            {

              await CreateUser().ConfigureAwait(false);
              await CreateAdminApp().ConfigureAwait(false);

            }
            catch (Exception ex)
            {
              Console.WriteLine($"Unhandled exception! {ex}");
            }
            finally
            {
              // Stop the application once the work is done
              _appLifetime.StopApplication();
            }
          });
        });

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }

    private async Task CreateUser()
    {
      await Task.Delay(10).ConfigureAwait(false);
      Console.WriteLine("Enter username of admin:");
      string? userName;
      while (string.IsNullOrEmpty(userName = Console.ReadLine()))
      {
        Console.WriteLine("Username can not be empty. Please username:");
      }

      if (await _identityRepository.UserExists(userName).ConfigureAwait(false))
      {
        Console.WriteLine("User already exists. Start again.", ConsoleColor.Red);
        _appLifetime.StopApplication();
        return;
      }

      Console.WriteLine("Enter user password:");
      string password = string.Empty;
      while (true)
      {
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Enter)
          break;
        password += key.KeyChar;
      }

      if (password.Length < 12)
      {
        Console.WriteLine("Password must contain at least 12 characters.", ConsoleColor.Red);
        _appLifetime.StopApplication();
        return;
      }

      Console.WriteLine("Repeat user password:");
      string password2 = string.Empty;
      while (true)
      {
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Enter)
          break;
        password2 += key.KeyChar;
      }

      if (password != password2)
      {
        Console.WriteLine("Passwords do not match.", ConsoleColor.Red);
        _appLifetime.StopApplication();
        return;
      }

      var user = await _identityRepository.CreateNewUser(userName, password).ConfigureAwait(false);
      await _identityRepository.UpdateUserPassword(user.Id, password, password).ConfigureAwait(false);
      Console.WriteLine($"User {userName} created.");
    }

    private async Task CreateAdminApp()
    {
      if (_clientRepository.AdminAppExists())
      {
        Console.WriteLine("Admin app exists already skipping.");
        _appLifetime.StopApplication();
      }

      var clientId = await _clientRepository.CreateAdminApp().ConfigureAwait(false);
      Console.WriteLine("Admin app created. Please copy id into the client application.");
      Console.WriteLine(clientId);
    }
  }
}
