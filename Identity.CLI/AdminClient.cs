using Identity.Application.Contracts;
using System;
using System.Threading.Tasks;

namespace Identity.CLI
{
  /// <summary>
  /// Contains function for creating, listing and deleting admin app clients
  /// </summary>
  public class AdminClient
  {
    private readonly IClientRepository _clientRepository;

    public AdminClient(IClientRepository clientRepository)
    {
      _clientRepository = clientRepository;
    }

    /// <summary>
    /// Creates a new admin app client
    /// </summary>
    public async Task CreateAdminAppClient()
    {
      var client = await _clientRepository.CreateAdminApp().ConfigureAwait(false);
      Console.WriteLine(client);
    }

    /// <summary>
    /// Lists all available admin app clients
    /// </summary>
    /// <returns></returns>
    public void ListAdminAppClients()
    {
      var clients = _clientRepository.ListAdminClientIds();
      foreach (var client in clients)
      {
        Console.WriteLine(client);
      }
    }

    /// <summary>
    /// Removes one admin app client
    /// </summary>
    /// <param name="clientId">Id of the client to be removed</param>
    public void DeleteAdminAppClient(Guid clientId)
    {
      _clientRepository.DeleteClient(clientId.ToString());
      Console.WriteLine($"Client {clientId} deleted!");
    }

  }
}
