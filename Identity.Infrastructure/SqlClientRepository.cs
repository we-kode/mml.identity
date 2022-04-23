using Identity.Application.Contracts;
using Identity.Application.Models;
using Identity.DBContext;
using OpenIddict.Abstractions;

namespace Identity.Infrastructure
{
  public class SqlClientRepository : IClientRepository
  {

    private readonly Func<ApplicationDBContext> _contextFactory;

    public SqlClientRepository(Func<ApplicationDBContext> contextFactory)
    {
      _contextFactory = contextFactory;
    }

    public IList<Client> ListClients(string? filter)
    {
      using var context = _contextFactory();
      return context.Applications.Where(app => !string.IsNullOrEmpty(app.Permissions) && app.Permissions.Contains(OpenIddictConstants.GrantTypes.ClientCredentials))
        .Select(app => new Client(app.ClientId ?? "", app.DisplayName ?? ""))
        .ToList();
    }

    public void DeleteClient(string id)
    {
      using var context = _contextFactory();
      var client = context.Applications.FirstOrDefault(app => !string.IsNullOrEmpty(app.ClientId) && app.ClientId == id);
      if (client == null)
      {
        return;
      }

      context.Applications.Remove(client);
      context.SaveChanges();
    }

    public bool Update(Client client)
    {
      using var context = _contextFactory();
      var clientToBeUpdated = context.Applications.FirstOrDefault(app => !string.IsNullOrEmpty(app.ClientId) && app.ClientId == client.ClientId);
      if (clientToBeUpdated == null)
      {
        return false;
      }
      clientToBeUpdated.DisplayName = client.DisplayName;
      context.SaveChanges();
      return true;
    }
  }
}
