using Identity.Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IdentityService.Test
{
  public class IdentityControllerTest
  {
    private readonly HttpClient client;
    private readonly WebApplicationFactory<Program> application;
    private string refreshToken = "";

    public IdentityControllerTest()
    {
      application = TestApplication.Build();
      client = application.CreateClient();
      client.DefaultRequestHeaders.Add("App-Key", "abc");
      Authorize().GetAwaiter().GetResult();
    }

    [Theory]
    [InlineData("test", "", false, HttpStatusCode.BadRequest)]
    [InlineData("test@newuser.test", "123456789012", false, HttpStatusCode.Created)]
    [InlineData("test@newuser.test", "123456789012", true, HttpStatusCode.Unauthorized)]
    public async Task Test_CRUD_One_User(string userName, string password, bool unauthorized, HttpStatusCode resultCode)
    {
      // create
      if (unauthorized)
      {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "xyz");
      }

      var payload = $"{{\"name\": \"{userName}\",\"password\": \"{password}\"}}";
      var content = new StringContent(payload, Encoding.UTF8, "application/json");
      var result = await client.PostAsync("/api/v1.0/identity/user/create", content);
      Assert.Equal(resultCode, result.StatusCode);
      if (result.IsSuccessStatusCode)
      {
        // read
        var createdUser = JsonConvert.DeserializeObject<User>(await result.Content.ReadAsStringAsync());
        var createdUserId = createdUser!.Id;
        result = await client.GetAsync($"/api/v1.0/identity/user/{createdUserId!}");
        var userResult = JsonConvert.DeserializeObject<User>(await result.Content.ReadAsStringAsync());
        Assert.True(result.IsSuccessStatusCode);
        Assert.Equal(createdUserId, userResult!.Id);

        // unique constraint
        result = await client.PostAsync("/api/v1.0/identity/user/create", content);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        // update
        payload = $"{{\"name\": \"{userName}abc\",\"password\": null}}";
        var uContent = new StringContent(payload, Encoding.UTF8, "application/json");
        result = await client.PostAsync($"/api/v1.0/identity/user/{createdUserId}", uContent);
        Assert.True(result.IsSuccessStatusCode);
        result = await client.GetAsync($"/api/v1.0/identity/user/{createdUserId}");
        userResult = JsonConvert.DeserializeObject<User>(await result.Content.ReadAsStringAsync());
        Assert.True(result.IsSuccessStatusCode);
        Assert.Equal($"{userName}abc", userResult!.Name);
        Assert.True(userResult.IsAdmin);

        //Delete
        result = await client.DeleteAsync($"/api/v1.0/identity/user/{createdUserId}");
        Assert.True(result.IsSuccessStatusCode);
        result = await client.GetAsync($"/api/v1.0/identity/user/{createdUserId}");
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        result = await client.DeleteAsync($"/api/v1.0/identity/user/{createdUserId}");
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);

        var serviceScope = application.Services.CreateScope();
        var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<long>>>();
        var adminUser = await userManager.FindByIdAsync(1.ToString());
        await (userManager.RemoveFromRoleAsync(adminUser!, Identity.Application.IdentityConstants.Roles.Admin));
        await Authorize();
        result = await client.PostAsync("/api/v1.0/identity/user/create", content);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        await userManager.AddToRoleAsync(adminUser!, Identity.Application.IdentityConstants.Roles.Admin);
      }
    }

    [Fact]
    public async Task Test_DeleteList()
    {
      var serviceScope = application.Services.CreateScope();
      var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<long>>>();
      var user1 = new IdentityUser<long> { Id = 42, UserName = "u1" };
      var user2 = new IdentityUser<long> { Id = 43, UserName = "u2" };
      var user3 = new IdentityUser<long> { Id = 44, UserName = "u3" };
      await userManager.CreateAsync(user1);
      await userManager.CreateAsync(user2);
      await userManager.CreateAsync(user3);

      var payload = $"[ 42,43,44]";
      var uContent = new StringContent(payload, Encoding.UTF8, "application/json");
      var result = await client.PostAsync($"/api/v1.0/identity/user/deleteList", uContent);
      Assert.True(result.IsSuccessStatusCode);

      Assert.False(userManager.Users.Any(u => u.Id == 42 || u.Id == 43 || u.Id == 44));
    }

    [Theory]
    [InlineData("test@user.test1", false, HttpStatusCode.OK)]
    [InlineData("test@user.test1", true, HttpStatusCode.Unauthorized)]
    public async Task Test_UpdateSettings(string newUsername, bool unauthorized, HttpStatusCode code)
    {
      if (unauthorized)
      {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "xyz");
      }

      var payload = $"{{\"name\": \"{newUsername}\"}}";
      var content = new StringContent(payload, Encoding.UTF8, "application/json");
      var result = await client.PostAsync("/api/v1.0/identity/user", content);

      Assert.Equal(code, result.StatusCode);

      if (result.IsSuccessStatusCode)
      {
        result = await client.GetAsync("/api/v1.0/identity/connect/userinfo");
        var userSettings = JsonConvert.DeserializeObject<User>(await result.Content.ReadAsStringAsync());
        Assert.Equal(newUsername, userSettings!.Name);

        payload = $"{{\"name\": \"{TestApplication.UserName}\"}}";
        content = new StringContent(payload, Encoding.UTF8, "application/json");
        await client.PostAsync("/api/v1.0/identity/user", content);
      }
    }

    [Theory]
    [InlineData("test@user.test", "secret123456", "pass0987654321", false, HttpStatusCode.OK)]
    [InlineData("test@user.test", "secret123456", "pass", false, HttpStatusCode.BadRequest)]
    [InlineData("test@user.test", "secret90876", "pass09876", false, HttpStatusCode.BadRequest)]
    [InlineData("test@user.test", "secret123456", "pass0987654321", true, HttpStatusCode.Unauthorized)]
    public async Task Test_UpdatePassword(string newUsername, string oldPassword, string newPassword, bool unauthorized, HttpStatusCode code)
    {
      if (unauthorized)
      {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "xyz");
      }

      var payload = $"{{\"name\": \"{newUsername}\", \"oldPassword\": \"{oldPassword}\", \"newPassword\": \"{newPassword}\"}}";
      var content = new StringContent(payload, Encoding.UTF8, "application/json");
      var result = await client.PostAsync("/api/v1.0/identity/user", content);

      Assert.Equal(code, result.StatusCode);

      if (result.IsSuccessStatusCode)
      {
        payload = $"{{\"name\": \"{TestApplication.UserName}\", \"oldPassword\": \"pass0987654321\", \"newPassword\": \"{TestApplication.Password}\"}}";
        content = new StringContent(payload, Encoding.UTF8, "application/json");
        await client.PostAsync("/api/v1.0/identity/user", content);
      }
    }

    [Fact]
    public async Task Test_RefreshTokens()
    {
      var newRefreshToken = refreshToken;
      var param = new KeyValuePair<string, string>("refresh_token", newRefreshToken);

      // send auth request
      var payload = new List<KeyValuePair<string, string>>
      {
        new("grant_type", "refresh_token"),
        new("client_id", "testClient"),
        new("scope", "offline_access"),
        param
      };

      // set tokens
      var result = await (await client.PostAsync("/api/v1.0/identity/connect/token", new FormUrlEncodedContent(payload))).Content.ReadAsStringAsync();
      dynamic token = JObject.Parse(result);
      newRefreshToken = token.refresh_token;
      Assert.NotEqual(newRefreshToken, refreshToken);
      payload.Remove(param);
      payload.Add(new KeyValuePair<string, string>("refresh_token", newRefreshToken));
      var result2 = await client.PostAsync("/api/v1.0/identity/connect/token", new FormUrlEncodedContent(payload));
      Assert.True(result2.IsSuccessStatusCode);
      await Task.Delay(10000);
      result2 = await client.PostAsync("/api/v1.0/identity/connect/token", new FormUrlEncodedContent(payload));
      Assert.False(result2.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Test_UserInfo()
    {
      var result = await client.GetAsync("/api/v1.0/identity/connect/userinfo");
      result.EnsureSuccessStatusCode();
      var userSettings = JsonConvert.DeserializeObject<User>(await result.Content.ReadAsStringAsync());
      Assert.Equal(TestApplication.UserName, userSettings!.Name);
    }

    [Fact]
    public async Task Test_Logout()
    {
      var originalAuthorization = client.DefaultRequestHeaders.Authorization;

      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "xyz");

      var result = await client.PostAsync("/api/v1.0/identity/connect/logout", new FormUrlEncodedContent([]));
      Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);

      client.DefaultRequestHeaders.Authorization = originalAuthorization;

      result = await client.PostAsync("/api/v1.0/identity/connect/logout", new FormUrlEncodedContent([]));
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);

      result = await client.GetAsync("/api/v1.0/identity/connect/userinfo");
      Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
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
      var result = await client.PostAsync("/api/v1.0/identity/connect/token", new FormUrlEncodedContent(payload));
      Assert.True(result.IsSuccessStatusCode);
      dynamic token = JObject.Parse(await result.Content.ReadAsStringAsync());
      refreshToken = token.refresh_token;
      string accessToken = token.access_token;
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
  }
}
