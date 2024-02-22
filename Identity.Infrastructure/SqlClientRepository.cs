using CryptoHelper;
using Identity.Application.Contracts;
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
using DBGroup = Identity.DBContext.Models.Group;
using System.Text.RegularExpressions;
using AutoMapper;

namespace Identity.Infrastructure
{
  public class SqlClientRepository : IClientRepository
  {
    private readonly Func<ApplicationDBContext> _contextFactory;
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;

    public SqlClientRepository(
      Func<ApplicationDBContext> contextFactory,
      IGroupRepository groupRepository,
      IMapper mapper
    )
    {
      _contextFactory = contextFactory;
      _groupRepository = groupRepository;
      _mapper = mapper;
    }

    public Clients ListClients(TagFilter tagFilter, string? filter, int skip, int take)
    {
      using var context = _contextFactory();
      var query = context.Applications
        .Include(app => app.Groups)
        .Where(app => !string.IsNullOrEmpty(app.Permissions))
        .Where(app => EF.Functions.Like(app.Permissions!, $"%{OpenIddictConstants.GrantTypes.ClientCredentials}%"))
        .Where(app => string.IsNullOrEmpty(filter) || EF.Functions.ILike(app.DisplayName ?? "", $"%{filter}%"));

      if (tagFilter.Groups.Count > 0)
      {
        query = query.Where(c => c.Groups.Any(g => tagFilter.Groups.Contains(g.Id)));
      }

      DateTime oldestDate = DateTime.UtcNow.Subtract(new TimeSpan(24, 0, 0));
      if (tagFilter.OnlyNew)
      {
        query = query.Where(c => c.RegistrationDate >= oldestDate);
      }

      var count = query.Count();
      var clients = query
        .OrderBy(app => app.DisplayName)
        .Select(app => MapModel(app))
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

      var clientToBeUpdated = context.Applications
        .Include(app => app.Groups)
        .First(app => !string.IsNullOrEmpty(app.ClientId) && app.ClientId == client.ClientId);

      clientToBeUpdated.DisplayName = client.DisplayName;
      clientToBeUpdated.DeviceIdentifier = client.DeviceIdentifier;

      var addedGroups = client.Groups
        .Where(g => _groupRepository.GroupExists(g.Id).GetAwaiter().GetResult())
        .Where(g => !clientToBeUpdated.Groups.Select(cg => cg.Id).Contains(g.Id))
        .Select(g => new DBGroup
        {
          Id = g.Id,
          Name = g.Name,
          IsDefault = g.IsDefault
        })
        .ToArray();

      var deletedGroups = clientToBeUpdated.Groups
        .Where(g => !client.Groups.Select(cg => cg.Id).Contains(g.Id))
        .ToArray();

      foreach (var addedGroup in addedGroups)
      {
        clientToBeUpdated.Groups.Add(addedGroup);
      }

      foreach (var deletedGroup in deletedGroups)
      {
        clientToBeUpdated.Groups.Remove(deletedGroup);
      }

      context.Tokens.RemoveRange(context.Tokens.Where(token => token.Application == clientToBeUpdated));

      context.SaveChanges();
    }

    public string? GetPublicKey(string clientId)
    {
      using var context = _contextFactory();
      return context.Applications.FirstOrDefault(app => app.ClientId == clientId)?.PublicKey;
    }

    public async Task CreateClient(string clientId, string clientSecret, string b64PublicKey, string displayName, string deviceIdentifier)
    {
      using var context = _contextFactory();

      var defaultGroups = context.Groups
        .Where(g => g.IsDefault)
        .ToArray();

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
        DisplayName = displayName,
        DeviceIdentifier = deviceIdentifier,
        Groups = defaultGroups,
        RegistrationDate = DateTime.UtcNow,
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

    public IList<Guid> ListAdminClientIds()
    {
      using var context = _contextFactory();
      return context.Applications
        .Where(app => EF.Functions.ILike(app.Permissions ?? "", $"%{OpenIddictConstants.Permissions.GrantTypes.Password}%"))
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
      var client = context.Applications
        .Include(app => app.Groups)
        .First(app => !string.IsNullOrEmpty(app.ClientId) && app.ClientId == id);
      return MapModel(client);
    }

    public void UpdateTokenRequestDate(string clientId)
    {
      using var context = _contextFactory();
      var client = context.Applications.FirstOrDefault(app => !string.IsNullOrEmpty(app.ClientId) && app.ClientId == clientId);
      if (client == null)
      {
        return;
      }
      client.LastTokenRefreshDate = DateTime.UtcNow;
      context.SaveChanges();
    }

    private static Client MapModel(OpenIddictClientApplication client)
    {
      return new Client(
        client.ClientId ?? "",
        client.DisplayName ?? "",
        client.DeviceIdentifier,
        client.Groups.Select(g => new Application.Models.Group(
          g.Id, g.Name, g.IsDefault
        )).ToArray())
      {
        LastTokenRefreshDate = client.LastTokenRefreshDate
      };
    }

    public bool IsApiClient(string clientId, string clientSecret)
    {
      using var context = _contextFactory();
      var client = context.Applications.FirstOrDefault(app =>
        EF.Functions.ILike(app.Permissions ?? "", $"%{OpenIddictConstants.Permissions.Endpoints.Introspection}%") &&
        !string.IsNullOrEmpty(app.ClientId) && app.ClientId == clientId
      );
      return client != null && Crypto.VerifyHashedPassword(client.ClientSecret, clientSecret);
    }

    public IList<string> GetApiClients()
    {
      using var context = _contextFactory();
      var clients = context.Applications
        .Where(app => EF.Functions.ILike(app.Permissions ?? "", $"%{OpenIddictConstants.Permissions.Endpoints.Introspection}%"))
        .Select(app => app.ClientId)
        .ToList();

      return clients!;
    }

    public void Assign(List<string> clients, List<Guid> initGroups, List<Guid> groups)
    {
      using var context = _contextFactory();
      var cAssing = context.Applications
        .Include(app => app.Groups)
        .Where(app => !string.IsNullOrEmpty(app.ClientId) && clients.Contains(app.ClientId)).ToList();
      var gAssign = context.Groups
       .Where(g => groups.Contains(g.Id) || initGroups.Contains(g.Id));
      foreach (var client in cAssing)
      {
        client.Groups = gAssign.ToList();
      }
      context.SaveChanges();
    }

    public Groups GetAssignedGroups(List<string> clients)
    {
      using var context = _contextFactory();
      var groups = context.Groups
        .Where(g => g.Clients.Any(c => !string.IsNullOrEmpty(c.ClientId) && clients.Contains(c.ClientId)));

      var count = groups.Count();

      return new Groups
      {
        TotalCount = count,
        Items = _mapper.ProjectTo<Application.Models.Group>(groups).ToList(),
      };
    }
  }
}
