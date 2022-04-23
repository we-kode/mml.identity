using Identity.Application.Contracts;
using Identity.Application.Models;
using Identity.DBContext;
using OpenIddict.Abstractions;
using Identity.DBContext.Models;

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

    string? IClientRepository.GetPublicKey(string clientId)
    {
      using var context = _contextFactory();
      return context.Applications.FirstOrDefault(app => app.ClientId == clientId)?.PublicKey;
    }

    void IClientRepository.CreateClient(string clientId, string clientSecret, string b64PublicKey)
    {
      using var context = _contextFactory();
      var client = context.Applications.FirstOrDefault(app => app.ClientId == clientId);
      if (client != null)
      {
        throw new ArgumentException($"Client with id {clientId} exists already");
      }
      client = new OpenIddictClientApplication
      {
        ClientId = clientId,
        ClientSecret = clientSecret,
        PublicKey = b64PublicKey,
        Permissions =
        new {
          OpenIddictConstants.Permissions.Endpoints.Token,
          OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
        }.ToString()
      };
      context.Applications.Add(client);
      context.SaveChanges();
    }
  }
}
