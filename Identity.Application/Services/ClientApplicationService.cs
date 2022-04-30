using Identity.Application.Contracts;
using Identity.Application.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Identity.Application.Services
{
  public class ClientApplicationService
  {
    private readonly IClientRepository _repository;
    private const string secretChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmopqrstuvwxyz+/-#!$%&()=?[]{}§<>,.;:_*";
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
    public async Task<ApplicationClient> CreateClient(string b64PublicKey)
    {
      using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
      var random = new Random();
      var secret = new string(Enumerable.Repeat(secretChars, secretLength).Select(s => s[random.Next(s.Length)]).ToArray());
      var client = new ApplicationClient(Guid.NewGuid().ToString(), secret);
      await _repository.CreateClient(client.ClientId, client.ClientSecret, b64PublicKey).ConfigureAwait(false);
      scope.Complete();
      return client;
    }
  }
}
