using Identity.Application.Contracts;
using Identity.Application.Models;
using Identity.Sockets;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IdentityService.Test
{
  public class ClientControllerTests
  {
    private readonly HttpClient client;
    private readonly WebApplicationFactory<Program> application;
    private string refreshToken = "";

    public ClientControllerTests()
    {
      application = TestApplication.Build();
      client = application.CreateClient();
      client.DefaultRequestHeaders.Add("App-Key", "abc");
      _Authorize().GetAwaiter().GetResult();
    }

    private async Task _Authorize()
    {
      // send auth request
      var payload = new List<KeyValuePair<string, string>>();
      payload.Add(new KeyValuePair<string, string>("grant_type", "password"));
      payload.Add(new KeyValuePair<string, string>("client_id", "testClient"));
      payload.Add(new KeyValuePair<string, string>("scope", "offline_access"));
      payload.Add(new KeyValuePair<string, string>("username", TestApplication.UserName));
      payload.Add(new KeyValuePair<string, string>("password", TestApplication.Password));

      // set tokens
      var result = await client.PostAsync("/api/v1.0/identity/connect/token", new FormUrlEncodedContent(payload)).Result.Content.ReadAsStringAsync();
      dynamic token = JObject.Parse(result);
      refreshToken = token.refresh_token;
      string accessToken = token.access_token;
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    [Fact]
    public async void Test_ClientUpdate()
    {
      // update existing client
      var clientId = Guid.NewGuid();
      var payload = $"{{\"clientId\": \"{clientId}\", \"displayName\": \"abc\"}}";
      var content = new StringContent(payload, Encoding.UTF8, "application/json");
      var result = await client.PostAsync("/api/v1.0/identity/client", content);
      Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);

      using var scope = application.Services.CreateScope();
      var scopedServices = scope.ServiceProvider;
      var clientRepository = scopedServices.GetRequiredService<IClientRepository>();
      await clientRepository.CreateClient(clientId.ToString(), "123", "test").ConfigureAwait(false);

      result = await client.PostAsync("/api/v1.0/identity/client", content);
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);
      Assert.Contains(clientRepository.ListClients(null).Items, client => client.DisplayName == "abc");
    }

    [Fact]
    public async Task Test_ListClients()
    {
      using var scope = application.Services.CreateScope();
      var scopedServices = scope.ServiceProvider;
      var clientRepository = scopedServices.GetRequiredService<IClientRepository>();
      await clientRepository.CreateClient("abc", "123", "test").ConfigureAwait(false);
      clientRepository.Update(new Client("abc", "abc"));
      await clientRepository.CreateClient("def", "123", "test").ConfigureAwait(false);
      clientRepository.Update(new Client("def", "def"));
      await clientRepository.CreateClient("ghi", "123", "test").ConfigureAwait(false);
      clientRepository.Update(new Client("ghi", "ghi"));

      var result = await client.GetAsync("/api/v1.0/identity/client/list");
      result.EnsureSuccessStatusCode();
      var clients = JsonConvert.DeserializeObject<Clients>(await result.Content.ReadAsStringAsync());
      Assert.True(clients.TotalCount >= 3);

      result = await client.GetAsync("/api/v1.0/identity/client/list?filter=d");
      clients = JsonConvert.DeserializeObject<Clients>(await result.Content.ReadAsStringAsync());
      Assert.Equal(1, clients.TotalCount);
      Assert.Contains(clients.Items, client => client.ClientId == "def");
    }

    [Fact]
    public async Task Test_DeleteClients()
    {
      var result = await client.DeleteAsync($"/api/v1.0/identity/client/abc");
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);

      using var scope = application.Services.CreateScope();
      var scopedServices = scope.ServiceProvider;
      var clientRepository = scopedServices.GetRequiredService<IClientRepository>();
      await clientRepository.CreateClient("clientToDelete", "123", "test").ConfigureAwait(false);
      result = await client.DeleteAsync($"/api/v1.0/identity/client/clientToDelete");
      result.EnsureSuccessStatusCode();

      Assert.Equal(0, clientRepository.ListClients("clientToDelete").TotalCount);
    }

    [Fact]
    public async void Test_RegisterClient()
    {
      var someRandomString = Convert.ToBase64String(Encoding.UTF8.GetBytes("somerandomstring"));
      var payload = $"{{\"base64PublicKey\": \"{someRandomString}\"}}";
      var content = new StringContent(payload, Encoding.UTF8, "application/json");
      var result = await client.PostAsync("/api/v1.0/identity/client/register/abcdef", content);
      var hubFinished = false;
      var apiFinished = false;
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);

      string token = string.Empty;
      var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hub/client", options =>
            {
              options.HttpMessageHandlerFactory = _ => application.Server.CreateHandler();
              options.Headers.Add("App-Key", "abc");
              options.Headers.Add("Authorization", client.DefaultRequestHeaders.Authorization!.ToString());
              options.CloseTimeout = TimeSpan.FromMinutes(5);
            })
            .Build();
      connection.On<RegistrationInformation>("REGISTER_TOKEN_UPDATED", t =>
      {
        Assert.True(!string.IsNullOrEmpty(t.Token));
        Assert.Equal("def", t.AppKey);
        token = t.Token;
      });
      connection.On<string>("CLIENT_REGISTERED", clientId =>
      {
        Assert.True(Guid.TryParse(clientId, out Guid _));
        hubFinished = true;
      });
      await connection.StartAsync().ConfigureAwait(false);
      await connection.InvokeAsync("SubscribeToClientRegistration").ConfigureAwait(false);
      while (string.IsNullOrEmpty(token))
      {
        await Task.Delay(100).ConfigureAwait(false);
      }
      result = await client.PostAsync($"/api/v1.0/identity/client/register/{token}", content);
      Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

      var rsa = RSA.Create();
      var pubKey = rsa.ExportRSAPublicKey();
      payload = $"{{\"base64PublicKey\": \"{Convert.ToBase64String(pubKey)}\"}}";
      content = new StringContent(payload, Encoding.UTF8, "application/json");
      result = await client.PostAsync($"/api/v1.0/identity/client/register/{token}", content);
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
      token = "";
      while (string.IsNullOrEmpty(token))
      {
        await Task.Delay(100).ConfigureAwait(false);
      }
      result = await client.PostAsync($"/api/v1.0/identity/client/register/{token}", content);
      var appClient = JsonConvert.DeserializeObject<ApplicationClient>(await result.Content.ReadAsStringAsync());
      Assert.True(appClient != null);
      Assert.True(!string.IsNullOrEmpty(appClient!.ClientSecret));
      Assert.True(!string.IsNullOrEmpty(appClient!.ClientId));
      apiFinished = true;
      while (!apiFinished && !hubFinished)
      {
        await Task.Delay(100).ConfigureAwait(false);
      }
      await connection.StopAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task Test_AuthorizeClient()
    {
      client.DefaultRequestHeaders.Remove("App-Key");
      client.DefaultRequestHeaders.Add("App-Key", "def");
      client.DefaultRequestHeaders.Remove("Authorization");

      using var scope = application.Services.CreateScope();
      var scopedServices = scope.ServiceProvider;
      var clientRepository = scopedServices.GetRequiredService<IClientRepository>();
      await clientRepository.CreateClient("testClient1", "testSecret1", "").ConfigureAwait(false);

      // send auth request
      // try to auth without signature
      var payload = new List<KeyValuePair<string, string>>();
      payload.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
      payload.Add(new KeyValuePair<string, string>("client_id", "testClient1"));
      payload.Add(new KeyValuePair<string, string>("client_secret", "testSecret1"));

      // set tokens
      var result = await client.PostAsync("/api/v1.0/identity/connect/token", new FormUrlEncodedContent(payload)).ConfigureAwait(false);
      Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);

      RSA rsa = RSA.Create();
      var pubKey = rsa.ExportRSAPublicKey();
      await clientRepository.CreateClient("testClient2", "testSecret2", Convert.ToBase64String(pubKey)).ConfigureAwait(false);

      // auth valid signature
      var signatureString = $"{{ \"clientId\" : \"testClient2\", \"clientSecret\" : \"testSecret2\", \"grant_type\" : \"client_credentials\" }}";
      payload = new List<KeyValuePair<string, string>>();
      payload.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
      payload.Add(new KeyValuePair<string, string>("client_id", "testClient2"));
      payload.Add(new KeyValuePair<string, string>("client_secret", "testSecret2"));
      var signature = new KeyValuePair<string, string>("code_challenge", Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(signatureString), HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1)));
      payload.Add(signature);
      var tokenResult = await client.PostAsync("/api/v1.0/identity/connect/token", new FormUrlEncodedContent(payload)).Result.Content.ReadAsStringAsync();
      dynamic token = JObject.Parse(tokenResult);
      string accessToken = token.access_token;
      Assert.True(!string.IsNullOrEmpty(accessToken));

      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
      var resultList = await client.GetAsync("/api/v1.0/identity/client/list");
      Assert.Equal(HttpStatusCode.Forbidden, resultList.StatusCode);

      // try with invalid signature
      payload.Remove(signature);
      payload.Add(new KeyValuePair<string, string>("code_challenge", Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes("random string"), HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1))));
      result = await client.PostAsync("/api/v1.0/identity/connect/token", new FormUrlEncodedContent(payload));
      Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }
  }
}
