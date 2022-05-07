using Identity.Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
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
      _Authorize().GetAwaiter().GetResult();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("test@user.test", 1)]
    [InlineData("TEST@USER.test", 1)]
    [InlineData("rrrrrrrrrrrrrrrrrrrrrrr", 0)]
    public async void Test_List(string? filter, int count = -1)
    {
      var filterString = string.IsNullOrEmpty(filter) ? "" : $"?filter={filter}";
      var result = await client.GetAsync($"/api/v1.0/identity/user/list{filterString}");
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);
      var users = JsonConvert.DeserializeObject<IList<User>>(await result.Content.ReadAsStringAsync());
      Assert.True(count == -1 ? users.Count >= 0 : users.Count == count);
      if (users.Count > 0)
      {
        Assert.Contains(users, user => user.Name == TestApplication.UserName);
      }

      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "xyz");
      result = await client.GetAsync($"/api/v1.0/identity/user/list{filterString}");
      Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Theory]
    [InlineData("test", null, false, HttpStatusCode.BadRequest)]
    [InlineData("test@newuser.test", "123456789012", false, HttpStatusCode.Created)]
    [InlineData("test@newuser.test", "123456789012", true, HttpStatusCode.Unauthorized)]
    public async void Test_CRUD_One_User(string userName, string password, bool unauthorized, HttpStatusCode resultCode)
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
        var createdUserId = createdUser.Id;
        result = await client.GetAsync($"/api/v1.0/identity/user/{createdUserId!}");
        var userResult = JsonConvert.DeserializeObject<User>(await result.Content.ReadAsStringAsync());
        Assert.True(result.IsSuccessStatusCode);
        Assert.Equal(createdUserId, userResult.Id);

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
        Assert.Equal($"{userName}abc", userResult.Name);
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
        var adminUser = userManager.FindByIdAsync(1.ToString()).GetAwaiter().GetResult();
        userManager.RemoveFromRoleAsync(adminUser, Identity.Application.IdentityConstants.Roles.Admin).GetAwaiter().GetResult();
        _Authorize().GetAwaiter().GetResult();
        result = await client.PostAsync("/api/v1.0/identity/user/create", content);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        userManager.AddToRoleAsync(adminUser, Identity.Application.IdentityConstants.Roles.Admin).GetAwaiter().GetResult();
      }
    }

    [Theory]
    [InlineData("test@user.test1", false, HttpStatusCode.OK)]
    [InlineData("test@user.test1", true, HttpStatusCode.Unauthorized)]
    public async void Test_UpdateSettings(string newUsername, bool unauthorized, HttpStatusCode code)
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
        Assert.Equal(newUsername, userSettings.Name);

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
    public async void Test_UpdatePassword(string newUsername, string oldPassword, string newPassword, bool unauthorized, HttpStatusCode code)
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
    public async void Test_RefreshTokens()
    {
      var newRefreshToken = refreshToken;
      var param = new KeyValuePair<string, string>("refresh_token", newRefreshToken);

      // send auth request
      var payload = new List<KeyValuePair<string, string>>();
      payload.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));
      payload.Add(new KeyValuePair<string, string>("client_id", "testClient"));
      payload.Add(new KeyValuePair<string, string>("scope", "offline_access"));
      payload.Add(param);

      // set tokens
      var result = await client.PostAsync("/api/v1.0/identity/connect/token", new FormUrlEncodedContent(payload)).Result.Content.ReadAsStringAsync();
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
    public async void Test_UserInfo()
    {
      var result = await client.GetAsync("/api/v1.0/identity/connect/userinfo");
      result.EnsureSuccessStatusCode();
      var userSettings = JsonConvert.DeserializeObject<User>(await result.Content.ReadAsStringAsync());
      Assert.Equal(TestApplication.UserName, userSettings.Name);
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
  }
}
