using CryptoHelper;
using Identity.Application.Contracts;
using Identity.Application.IdentityConstants;
using Identity.Application.Models;
using Identity.DBContext;
using Identity.DBContext.Models;
using OpenIddict.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure
{
  public class SqlClientRepository : IClientRepository
  {

    private readonly Func<ApplicationDBContext> _contextFactory;

    public SqlClientRepository(Func<ApplicationDBContext> contextFactory)
    {
      _contextFactory = contextFactory;
    }

    public Clients ListClients(string? filter, int skip, int take)
    {
      using var context = _contextFactory();
      var query = context.Applications
        .Where(app => !string.IsNullOrEmpty(app.Permissions) && !EF.Functions.Like(app.Permissions, $"%{Scopes.Upload}%"))
        .Where(app => EF.Functions.Like(app.Permissions!, $"%{OpenIddictConstants.GrantTypes.ClientCredentials}%"))
        .Where(app => string.IsNullOrEmpty(filter) || EF.Functions.ILike(app.DisplayName ?? "", $"%{filter}%"))
        .OrderBy(app => app.DisplayName);

      var count = query.Count();
      var clients = query
        .Select(app => new Client(app.ClientId ?? "", app.DisplayName ?? ""))
        .Skip(skip)
        .Take(take)
        .ToList();

      return new Clients
      {
        TotalCount = count,
        Items = clients
      };
    }

    public void DeleteClient(string id)
    {
      using var context = _contextFactory();
      var client = context.Applications.FirstOrDefault(app => !string.IsNullOrEmpty(app.ClientId) && app.ClientId == id);
      if (client == null)
      {
        return;
      }

      context.Tokens.RemoveRange(context.Tokens.Where(token => token.Application == client));
      context.Authorizations.RemoveRange(context.Authorizations.Where(authorization => authorization.Application == client));
      context.Applications.Remove(client);

      context.SaveChanges();
    }

    public void Update(Client client)
    {
      using var context = _contextFactory();
      var clientToBeUpdated = context.Applications.First(app => !string.IsNullOrEmpty(app.ClientId) && app.ClientId == client.ClientId);
      clientToBeUpdated.DisplayName = client.DisplayName;
      context.SaveChanges();
    }

    public string? GetPublicKey(string clientId)
    {
      using var context = _contextFactory();
      return context.Applications.FirstOrDefault(app => app.ClientId == clientId)?.PublicKey;
    }

    public async Task CreateClient(string clientId, string clientSecret, string b64PublicKey)
    {
      using var context = _contextFactory();
      var client = new OpenIddictClientApplication
      {
        ClientId = clientId,
        ClientSecret = Crypto.HashPassword(clientSecret),
        PublicKey = b64PublicKey,
        Permissions = JsonSerializer.Serialize(new[]
        {
          OpenIddictConstants.Permissions.Endpoints.Token,
          OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
        }),
        Type = OpenIddictConstants.ClientTypes.Confidential,
        DisplayName = $"device-{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}"
      };
      context.Applications.Add(client);
      await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public bool AdminAppExists()
    {
      using var context = _contextFactory();
      return context.Applications.Any(app => !string.IsNullOrEmpty(app.Permissions) && EF.Functions.Like(app.Permissions ?? "", $"%{OpenIddictConstants.GrantTypes.Password}%"));
    }

    public async Task<Guid> CreateAdminApp()
    {
      using var context = _contextFactory();
      var clientId = Guid.NewGuid();
      var client = new OpenIddictClientApplication
      {
        ClientId = clientId.ToString(),
        DisplayName = "Admin App",
        Permissions = JsonSerializer.Serialize(new[]
        {
          OpenIddictConstants.Permissions.Endpoints.Token,
          OpenIddictConstants.Permissions.Endpoints.Logout,
          OpenIddictConstants.Permissions.GrantTypes.Password,
          OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
          OpenIddictConstants.Scopes.OfflineAccess,
        }),
        Type = OpenIddictConstants.ClientTypes.Public
      };
      context.Applications.Add(client);
      await context.SaveChangesAsync().ConfigureAwait(false);
      return clientId;
    }

    public async Task CreateUploadClient(UploadClient client)
    {
      using var context = _contextFactory();
      var appClient = new OpenIddictClientApplication
      {
        ClientId = client.ClientId,
        ClientSecret = Crypto.HashPassword(client.ClientSecret),
        DisplayName = $"Upload client {client.ClientId}",
        Permissions = JsonSerializer.Serialize(new[]
        {
          OpenIddictConstants.Permissions.Endpoints.Token,
          OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
          OpenIddictConstants.Permissions.Prefixes.Scope + Scopes.Upload
        }),
        Type = OpenIddictConstants.ClientTypes.Confidential
      };
      context.Applications.Add(appClient);
      await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public IList<Guid> ListUploadClientIds()
    {
      using var context = _contextFactory();
      return context.Applications
        .Where(app => EF.Functions.ILike(app.Permissions ?? "", $"%{Scopes.Upload}%"))
        .Select(app => Guid.Parse(app.ClientId!))
        .ToList();
    }

    public bool ClientExists(string clientId)
    {
      using var context = _contextFactory();
      return context.Applications.Any(app => !string.IsNullOrEmpty(app.ClientId) && app.ClientId == clientId);
    }

    public Client GetClient(string id)
    {
      using var context = _contextFactory();
      var client = context.Applications.First(app => !string.IsNullOrEmpty(app.ClientId) && app.ClientId == id);
      return new Client(client.ClientId ?? "", client.DisplayName ?? "");
    }
  }
}
