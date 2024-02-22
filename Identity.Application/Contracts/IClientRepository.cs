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
    /// <param name="tagFilter">Tags on which the records will be filtered.</param>
    /// <param name="filter">Clients will be filtered by given string</param>
    /// <param name="skip">Elements to be skipped. default <see cref="List.Skip"/></param>
    /// <param name="filter">Elements to be loaded in one chunk. Default <see cref="List.Take"/></param>
    /// <returns><see cref="Clients"/></returns>
    Clients ListClients(TagFilter tagFilter, string? filter = null, int skip = List.Skip, int take = List.Take);

    /// <summary>
    /// Deletes one client
    /// </summary>
    /// <param name="id">Id of client to be deleted</param>
    void DeleteClient(string id);

    /// <summary>
    /// Lists the ids of the existing admin clients
    /// </summary>
    /// <returns><see cref="IList{T}"/> of client ids</returns>
    IList<Guid> ListAdminClientIds();

    /// <summary>
    /// Checks whether the given client is allowed to introspect the users access token.
    /// </summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="clientSecret">The secret of the client.</param>
    /// <returns>True, if client is allowed to introspect access tokens.</returns>
    bool IsApiClient(string clientId, string clientSecret);

    /// <summary>
    /// Returns List of api clients.
    /// </summary>
    /// <returns>List of client ids.</returns>
    IList<string> GetApiClients();

    /// <summary>
    /// Creates a new Client
    /// </summary>
    /// <param name="clientId">The client id</param>
    /// <param name="clientSecret">the client secret</param>
    /// <param name="b64PublicKey">base64 string of the public key</param>
    /// <param name="displayName">The name of the client which will be shown to admins.</param>
    /// <param name="deviceIdentifier">An identification of the device the client belongs to to differentiate between devices. E.g. the device name.</param>
    Task CreateClient(string clientId, string clientSecret, string b64PublicKey, string displayName, string deviceIdentifier);

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

    /// <summary>
    /// Assigns clients to groups.
    /// </summary>
    /// <param name="clients">Ids of clients.</param>
    /// <param name="initGroups">The ids of the init selected groups.</param>
    /// <param name="groups">Ids of groups.</param>
    void Assign(List<string> clients, List<Guid> initGroups, List<Guid> groups);
    
    /// <summary>
    /// Loads assinged groups of given clients.
    /// </summary>
    /// <param name="clients">Clients groups should be laoded.</param>
    /// <returns>Ids of assigned groups.</returns>
    Groups GetAssignedGroups(List<string> clients);
  }
}
