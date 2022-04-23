using Identity.Application.Models;
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
    /// Updates one clients display name
    /// </summary>
    /// <param name="client">Client to be updated</param>
    /// <returns>True, if update successfull</returns>
    bool Update(Client client);
  }
}
