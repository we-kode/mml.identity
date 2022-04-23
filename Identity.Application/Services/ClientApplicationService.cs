using Identity.Application.Contracts;
using Identity.Application.Models;
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
    /// <returns><see cref="ApplicationClient"/></returns>
    public ApplicationClient CreateClient(string b64PublicKey)
    {
      using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
      var random = new Random();
      var secret = new string(Enumerable.Repeat(secretChars, secretLength).Select(s => s[random.Next(s.Length)]).ToArray());
      var client = new ApplicationClient(Guid.NewGuid().ToString(), secret);
      _repository.CreateClient(client.ClientId, client.ClientSecret, b64PublicKey);
      scope.Complete();
      return client;
    }
  }
}
