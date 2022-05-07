using Identity.Application.Contracts;
using Identity.Application.Models;
using PasswordGenerator;
using System;
using System.Threading.Tasks;
using System.Transactions;

namespace Identity.Application.Services
{
  public class ClientApplicationService
  {
    private readonly IClientRepository _repository;
    private const int secretLength = 101;

    public ClientApplicationService(IClientRepository repository)
    {
      _repository = repository;
    }

    /// <summary>
    /// Generates new client
    /// </summary>
    /// <param name="b64PublicKey">the public key of the new client as base64 string</param>
    /// <returns><see cref="ApplicationClient"/></returns>
    public async Task<ApplicationClient?> CreateClient(string b64PublicKey)
    {
      using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
      var secret = new Password(secretLength).Next();
      var client = new ApplicationClient(Guid.NewGuid().ToString(), secret);
      if (_repository.ClientExists(client.ClientId))
      {
        return null;
      }
      await _repository.CreateClient(client.ClientId, client.ClientSecret, b64PublicKey).ConfigureAwait(false);
      scope.Complete();
      return client;
    }
  }
}
