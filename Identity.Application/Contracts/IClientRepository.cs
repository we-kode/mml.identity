using Identity.Application.IdentityConstants;
using Identity.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Identity.Application.Contracts
{
  public interface IClientRepository
  {
    /// <summary>
    /// Loads list of clients.
    /// </summary>
    /// <param name="filter">Clients will be filtered by given string</param>
    /// <param name="skip">Elements to be skipped. default <see cref="List.Skip"/></param>
    /// <param name="filter">Elements to be loaded in one chunk. Default <see cref="List.Take"/></param>
    /// <returns><see cref="Clients"/></returns>
    Clients ListClients(string? filter = null, int skip = List.Skip, int take = List.Take);

    /// <summary>
    /// Deletes one client
    /// </summary>
    /// <param name="id">Id of client to be deleted</param>
    void DeleteClient(string id);

    /// <summary>
    /// Saves a new uplaod client
    /// </summary>
    /// <param name="client">The <see cref="UploadClient"/> to be saved</param>
    Task CreateUploadClient(UploadClient client);

    /// <summary>
    /// Lists the ids of the existing upload clients
    /// </summary>
    /// <returns><see cref="IList{T}"/> of client ids</returns>
    IList<Guid> ListUploadClientIds();

    /// <summary>
    /// Creates a new Client
    /// </summary>
    /// <param name="clientId">The client id</param>
    /// <param name="clientSecret">the client secret</param>
    /// <param name="b64PublicKey">base64 string of the public key</param>
    /// <param name="displayName">The name of the client which will be shown to admins.</param>
    /// <param name="device">An identification of the device the client belongs to to differentiate between devices. E.g. the device name.</param>
    Task CreateClient(string clientId, string clientSecret, string b64PublicKey, string displayName, string device);

    /// <summary>
    /// Updates one clients display name
    /// </summary>
    /// <param name="client">Client to be updated</param>
    /// <returns>True, if update successfull</returns>
    void Update(Client client);

    /// <summary>
    /// Determines if client exists
    /// </summary>
    /// <param name="clientId">id of the client</param>
    /// <returns>True, if client exists</returns>
    bool ClientExists(string clientId);

    /// <summary>
    /// Loads the saved base64 public key string of the client id
    /// </summary>
    /// <param name="clientId">Id of the client</param>
    /// <returns>Base64 string of the public key or null if no public key is stored for client.</returns>
    string? GetPublicKey(string clientId);

    /// <summary>
    /// Determines if one admin app client is created already
    /// </summary>
    /// <returns>True if admin app client exists.</returns>
    bool AdminAppExists();

    /// <summary>
    /// Creates admin app and returns the Client id
    /// </summary>
    /// <returns><see cref="Guid"/> of the client id.</returns>
    Task<Guid> CreateAdminApp();

    /// <summary>
    /// Returns a client by id.
    /// </summary>
    /// <param name="id">The id of the client.</param>
    /// <returns><see cref="Client"/></returns>
    Client GetClient(string id);

    /// <summary>
    /// Sets the token request date to current <see cref="DateTime.UtcNow"></see>
    /// </summary>
    /// <param name="clientId">The id of the client to be updated.</param>
    void UpdateTokenRequestDate(string clientId);
  }
}
