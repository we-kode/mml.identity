using Identity.Application;
using Identity.Application.Models;
using Identity.DBContext;
using Identity.DBContext.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenIddict.Abstractions;
using System;
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
    private readonly string userName = "test@user.test";
    private readonly string password = "secret123456";
    private string refreshToken = "";

    public IdentityControllerTest()
    {
      var oAuthCLient = new OpenIddictApplicationDescriptor
      {
        ClientId = "testClient",
        DisplayName = "test",
      };
      oAuthCLient.Permissions.Add("ept:token");
      oAuthCLient.Permissions.Add("ept:logout");
      oAuthCLient.Permissions.Add("gt:password");
      oAuthCLient.Permissions.Add("gt:refresh_token");
      oAuthCLient.Permissions.Add("scp:offline_access");

      Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
      var configuration = new ConfigurationBuilder()
          .AddInMemoryCollection(new Dictionary<string, string>
          {
                    { "ADMIN_APP_KEY", "abc" }
          })
          .Build();

      application = new WebApplicationFactory<Program>()
          .WithWebHostBuilder(builder =>
          {
            builder.UseConfiguration(configuration);
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
                  {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDBContext>));

                    services.Remove(descriptor!);

                    var dbFactory = services.SingleOrDefault(s => s.ServiceType == typeof(Func<ApplicationDBContext>));

                    services.AddDbContext<ApplicationDBContext>(options =>
                          {
                            options.UseInMemoryDatabase("InMemoryDbForTesting");
                            options.UseOpenIddict<OpenIddictClientApplication, OpenIddictClientAuthorization, OpenIddictClientScope, OpenIddictClientToken, string>();
                          });

                    //seed first user
                    var serviceProvider = services.BuildServiceProvider();
                    using var scope = serviceProvider.CreateScope();
                    var scopedServices = scope.ServiceProvider;
                    var userManager = scopedServices.GetRequiredService<UserManager<IdentityUser<long>>>();
                    var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole<long>>>();
                    roleManager.CreateAsync(new IdentityRole<long>(Roles.ADMIN));
                    var seededUser = new IdentityUser<long>
                    {
                      UserName = userName,
                      NormalizedUserName = userName,
                      EmailConfirmed = true
                    };
                    var tu = userManager.CreateAsync(seededUser, password).GetAwaiter().GetResult();
                    var tr = userManager.AddToRoleAsync(seededUser, Roles.ADMIN).GetAwaiter().GetResult();

                    var manager = scopedServices.GetRequiredService<IOpenIddictApplicationManager>();
                    var existingClientApp = manager.FindByClientIdAsync(oAuthCLient.ClientId!).GetAwaiter().GetResult();
                    if (existingClientApp == null)
                    {
                      manager.CreateAsync(oAuthCLient).GetAwaiter().GetResult();
                    }

                    // TODO register db fyctory here as in memeory


                  });
          });

      client = application.CreateClient();
      client.DefaultRequestHeaders.Add("App-Key", "abc");
      _Authorize().GetAwaiter().GetResult();
    }

    [Fact]
    public async void Test_Get()
    {
      var result = await client.GetAsync("/api/v1.0/identity/user");
      result.EnsureSuccessStatusCode();
      var userSettings = JsonConvert.DeserializeObject<User>(await result.Content.ReadAsStringAsync());
      Assert.Equal(userName, userSettings.Name);
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "xyz");
      result = await client.GetAsync("/api/v1.0/identity/user");
      Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
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
        Assert.Contains(users, user => user.Name == userName);
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
        userManager.RemoveFromRoleAsync(adminUser, Roles.ADMIN).GetAwaiter().GetResult();
        _Authorize().GetAwaiter().GetResult();
        result = await client.PostAsync("/api/v1.0/identity/user/create", content);
        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        userManager.AddToRoleAsync(adminUser, Roles.ADMIN).GetAwaiter().GetResult();
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
        result = await client.GetAsync("/api/v1.0/identity/user");
        var userSettings = JsonConvert.DeserializeObject<User>(await result.Content.ReadAsStringAsync());
        Assert.Equal(newUsername, userSettings.Name);

        payload = $"{{\"name\": \"{userName}\"}}";
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
        payload = $"{{\"name\": \"{userName}\", \"oldPassword\": \"pass0987654321\", \"newPassword\": \"{password}\"}}";
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
      Assert.Equal(userName, userSettings.Name);
    }

    private async Task _Authorize()
    {
      // send auth request
      var payload = new List<KeyValuePair<string, string>>();
      payload.Add(new KeyValuePair<string, string>("grant_type", "password"));
      payload.Add(new KeyValuePair<string, string>("client_id", "testClient"));
      payload.Add(new KeyValuePair<string, string>("scope", "offline_access"));
      payload.Add(new KeyValuePair<string, string>("username", userName));
      payload.Add(new KeyValuePair<string, string>("password", password));

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
      // update not existing client
      var payload = $"{{\"clientId\": \"abbc\", \"displayName\": \"abc\"}}";
      var content = new StringContent(payload, Encoding.UTF8, "application/json");
      var result = await client.PostAsync("/api/v1.0/identity/client", content);
      Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async void Test_ListClients()
    {
      // add clients and than get them over api
      var result = await client.GetAsync("/api/v1.0/identity/client/list");
      result.EnsureSuccessStatusCode();
    }

    [Fact]
    public async void Test_DeleteClients()
    {
      // delete exiting client
      // delet not existing client
      // delete as non user
      var result = await client.GetAsync("/api/v1.0/identity/client/list");
      result.EnsureSuccessStatusCode();
    }

    [Fact]
    public async void Test_AuthorizeClient()
    {
      // try to auth wihtout signature
      // auth valid signature
      var result = await client.GetAsync("/api/v1.0/identity/client/list");
      result.EnsureSuccessStatusCode();
    }
  }
}
