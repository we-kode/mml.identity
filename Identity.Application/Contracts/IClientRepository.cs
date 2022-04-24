using Identity.Application.Models;
using System.Collections.Generic;

namespace Identity.Application.Contracts
{
  public interface IClientRepository
  {
    /// <summary>
    /// Loads list of clients.
    /// </summary>
    /// <param name="filter">Clients will be filtered by given string</param>
    /// <returns>List of <see cref="Client"/></returns>
    IList<Client> ListClients(string? filter);

    /// <summary>
    /// Deletes one client
    /// </summary>
    /// <param name="id">Id of client to be deleted</param>
    void DeleteClient(string id);

    /// <summary>
    /// Creates a new Client
    /// </summary>
    /// <param name="clientId">The client id</param>
    /// <param name="clientSecret">the client secret</param>
    /// <param name="b64PublicKey">base64 string of the public key</param>
    void CreateClient(string clientId, string clientSecret, string b64PublicKey);

    /// <summary>
    /// Updates one clients display name
    /// </summary>
    /// <param name="client">Client to be updated</param>
    /// <returns>True, if update successfull</returns>
    bool Update(Client client);

    /// <summary>
    /// Loads the saved base64 public key string of the client id
    /// </summary>
    /// <param name="clientId">Id of the client</param>
    /// <returns>Base64 string of the public key or null if no public key is stored for client.</returns>
    string? GetPublicKey(string clientId);
  }
}
