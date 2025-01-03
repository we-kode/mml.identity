﻿using Identity.Application.Contracts;
using Identity.Application.Models;
using Identity.Sockets;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
      Authorize().GetAwaiter().GetResult();
    }

    private async Task Authorize()
    {
      // send auth request
      var payload = new List<KeyValuePair<string, string>>
      {
        new("grant_type", "password"),
        new("client_id", "testClient"),
        new("scope", "offline_access"),
        new("username", TestApplication.UserName),
        new("password", TestApplication.Password)
      };

      // set tokens
      var result = await client.PostAsync("/api/v1.0/identity/connect/token", new FormUrlEncodedContent(payload)).Result.Content.ReadAsStringAsync();
      dynamic token = JObject.Parse(result);
      refreshToken = token.refresh_token;
      string accessToken = token.access_token;
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    [Fact]
    public async Task Test_ClientUpdate()
    {
      // update existing client
      var clientId = Guid.NewGuid();
      var payload = $"{{\"clientId\": \"{clientId}\", \"displayName\": \"abc\", \"deviceIdentifier\": \"apple\"}}";
      var content = new StringContent(payload, Encoding.UTF8, "application/json");
      var result = await client.PostAsync("/api/v1.0/identity/client", content);
      Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);

      using var scope = application.Services.CreateScope();
      var scopedServices = scope.ServiceProvider;
      var clientRepository = scopedServices.GetRequiredService<IClientRepository>();
      await clientRepository.CreateClient(clientId.ToString(), "123", "test", "ads", "iphone");

      result = await client.PostAsync("/api/v1.0/identity/client", content);
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);
      Assert.Contains(clientRepository.ListClients(new()).Items, client => client.DisplayName == "abc");
    }

    [Fact]
    public async Task Test_DeleteClients()
    {
      var result = await client.DeleteAsync($"/api/v1.0/identity/client/abc");
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);

      using var scope = application.Services.CreateScope();
      var scopedServices = scope.ServiceProvider;
      var clientRepository = scopedServices.GetRequiredService<IClientRepository>();
      await clientRepository.CreateClient("clientToDelete", "123", "test", "ads", "apple");
      result = await client.DeleteAsync($"/api/v1.0/identity/client/clientToDelete");
      result.EnsureSuccessStatusCode();

      Assert.Equal(0, clientRepository.ListClients(new TagFilter(), "clientToDelete").TotalCount);
    }

    [Fact]
    public async Task Test_DeleteList()
    {
      using var scope = application.Services.CreateScope();
      var scopedServices = scope.ServiceProvider;
      var clientRepository = scopedServices.GetRequiredService<IClientRepository>();
      await clientRepository.CreateClient("clientToDelete", "123", "test", "ads", "apple");
      await clientRepository.CreateClient("clientToDelete2", "123", "test", "ads", "apple");
      await clientRepository.CreateClient("clientToDelete3", "123", "test", "ads", "apple");

      var payload = $"[ \"clientToDelete\",\"clientToDelete2\",\"clientToDelete3\"]";
      var uContent = new StringContent(payload, Encoding.UTF8, "application/json");
      var result = await client.PostAsync($"/api/v1.0/identity/client/deleteList", uContent);
      Assert.True(result.IsSuccessStatusCode);

      Assert.DoesNotContain(clientRepository.ListClients(new TagFilter()).Items, u => u.ClientId == "clientToDelete" || u.ClientId == "clientToDelete2" || u.ClientId == "clientToDelete3");
    }

    [Fact]
    public async Task Test_RegisterClient()
    {
      var someRandomString = Convert.ToBase64String(Encoding.UTF8.GetBytes("somerandomstring"));
      var payload = $"{{\"base64PublicKey\": \"{someRandomString}\", \"displayName\": \"abc\", \"deviceIdentifier\": \"iphone\"}}";
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
        Assert.False(string.IsNullOrEmpty(t.Token));
        Assert.Equal("def", t.AppKey);
        token = t.Token;
      });
      connection.On<string>("CLIENT_REGISTERED", clientId =>
      {
        Assert.True(Guid.TryParse(clientId, out Guid _));
        hubFinished = true;
      });
      await connection.StartAsync();
      await connection.InvokeAsync("SubscribeToClientRegistration");
      while (string.IsNullOrEmpty(token))
      {
        await Task.Delay(100);
      }
      result = await client.PostAsync($"/api/v1.0/identity/client/register/{token}", content);
      Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

      var rsa = RSA.Create();
      var pubKey = rsa.ExportRSAPublicKey();
      payload = $"{{\"base64PublicKey\": \"{Convert.ToBase64String(pubKey)}\", \"displayName\": \"abc\", \"deviceIdentifier\": \"iphone\"}}";
      content = new StringContent(payload, Encoding.UTF8, "application/json");
      result = await client.PostAsync($"/api/v1.0/identity/client/register/{token}", content);
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
      token = "";
      while (string.IsNullOrEmpty(token))
      {
        await Task.Delay(100);
      }
      result = await client.PostAsync($"/api/v1.0/identity/client/register/{token}", content);
      var appClient = JsonConvert.DeserializeObject<ApplicationClient>(await result.Content.ReadAsStringAsync());
      Assert.NotNull(appClient);
      Assert.False(string.IsNullOrEmpty(appClient!.ClientSecret));
      Assert.False(string.IsNullOrEmpty(appClient!.ClientId));
      apiFinished = true;
      while (!apiFinished && !hubFinished)
      {
        await Task.Delay(100);
      }
      await connection.StopAsync();
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
      await clientRepository.CreateClient("testClient1", "testSecret1", "", "ads", "apple");

      // send auth request
      // try to auth without signature
      var payload = new List<KeyValuePair<string, string>>
      {
        new("grant_type", "client_credentials"),
        new("client_id", "testClient1"),
        new("client_secret", "testSecret1")
      };

      // set tokens
      var result = await client.PostAsync("/api/v1.0/identity/connect/token", new FormUrlEncodedContent(payload));
      Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);

      RSA rsa = RSA.Create();
      var pubKey = rsa.ExportRSAPublicKey();
      await clientRepository.CreateClient("testClient2", "testSecret2", Convert.ToBase64String(pubKey), "ads", "apple");

      // auth valid signature
      var clientTokenRequestDateOld = clientRepository.GetClient("testClient2").LastTokenRefreshDate;
      var signatureString = "{\"grant_type\":\"client_credentials\",\"client_id\":\"testClient2\",\"client_secret\":\"testSecret2\"}";
      payload =
      [
        new KeyValuePair<string, string>("grant_type", "client_credentials"),
        new KeyValuePair<string, string>("client_id", "testClient2"),
        new KeyValuePair<string, string>("client_secret", "testSecret2"),
      ];
      var signature = new KeyValuePair<string, string>("code_challenge", Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(signatureString), HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1)));
      payload.Add(signature);
      var tokenResult = await (await client.PostAsync("/api/v1.0/identity/connect/token", new FormUrlEncodedContent(payload))).Content.ReadAsStringAsync();
      dynamic token = JObject.Parse(tokenResult);
      string accessToken = token.access_token;
      Assert.False(string.IsNullOrEmpty(accessToken));
      var clientTokenRequestDateUpdated = clientRepository.GetClient("testClient2").LastTokenRefreshDate;
      Assert.True(clientTokenRequestDateUpdated > clientTokenRequestDateOld);

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
