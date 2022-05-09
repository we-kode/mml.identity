using Identity.Application.Contracts;
using System;
using System.Threading.Tasks;

namespace Identity.CLI
{
  /// <summary>
  /// Contains function for creating, listing and deleting upload clients
  /// </summary>
  public class UploadClient
  {
    private readonly IClientRepository _clientRepository;

    public UploadClient(IClientRepository clientRepository)
    {
      _clientRepository = clientRepository;
    }

    /// <summary>
    /// Creates a new upload client
    /// </summary>
    public async Task CreateUploadClient()
    {
      var client = new Application.Models.UploadClient();
      await _clientRepository.CreateUploadClient(client).ConfigureAwait(false);
      Console.WriteLine(client);
    }

    /// <summary>
    /// Lists all available upload clients
    /// </summary>
    /// <returns></returns>
    public void ListUploadClients()
    {
      var clients = _clientRepository.ListUploadClientIds();
      foreach (var client in clients)
      {
        Console.WriteLine(client);
      }
    }

    /// <summary>
    /// Removes one upload client
    /// </summary>
    /// <param name="clientId">Id of the client to be removed</param>
    public void DeleteUploadClient(Guid clientId)
    {
      _clientRepository.DeleteClient(clientId.ToString());
      Console.WriteLine($"Client {clientId} deleted!");
    }

  }
}
