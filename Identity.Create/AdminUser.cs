using Identity.Application.Contracts;
using System;
using System.Threading.Tasks;

namespace Identity.Create
{
  /// <summary>
  /// Functions to create one admin user
  /// </summary>
  public class AdminUser
  {

    private readonly IIdentityRepository _identityRepository;
    private readonly IClientRepository _clientRepository;

    public AdminUser(IIdentityRepository identiytRepository, IClientRepository clientRepository)
    {
      _identityRepository = identiytRepository;
      _clientRepository = clientRepository;
    }

    /// <summary>
    /// Creates one admin user
    /// </summary>
    /// <returns>True, if user created successfully</returns>
    public async Task<bool> CreateUser()
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
        return false;
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
        return false;
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
        return false;
      }

      var user = await _identityRepository.CreateNewUser(userName, password).ConfigureAwait(false);
      await _identityRepository.UpdateUserPassword(user.Id, password, password).ConfigureAwait(false);
      Console.WriteLine($"User {userName} created.");
      return true;
    }

    /// <summary>
    /// Cretaes admin app if no one exists
    /// </summary>
    public async Task CreateAdminApp()
    {
      if (_clientRepository.AdminAppExists())
      {
        Console.WriteLine("Admin app exists already skipping.");
        return;
      }

      var clientId = await _clientRepository.CreateAdminApp().ConfigureAwait(false);
      Console.WriteLine("Admin app created. Please copy id into the client application.");
      Console.WriteLine(clientId);
    }
  }
}
