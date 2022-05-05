using Identity.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using OpenIddict.Validation.AspNetCore;
using PasswordGenerator;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace Identity.Sockets
{
  /// <summary>
  /// Signalr hub to registrate new clients
  /// </summary>
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = IdentityConstants.Roles.Admin)]
  public class RegisterClientHub : Hub
  {

    private const string REGISTRATION_TOKEN_INTERVALL = "REGISTRATION_TOKEN_INTERVALL";

    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _cache;
    private readonly IHubContext<RegisterClientHub> _hubContext;

    public RegisterClientHub(IConfiguration configuration, IDistributedCache cache, IHubContext<RegisterClientHub> hubContext)
    {
      _configuration = configuration;
      _cache = cache;
      _hubContext = hubContext;
    }

    public async Task SubscribeToClientRegistration()
    {
      await Groups.AddToGroupAsync(Context.ConnectionId, Context.ConnectionId).ConfigureAwait(false);
      GenerateRegistrationToken(Context.ConnectionId);
    }

    public async Task UpdateRegistrationToken(string connectionId, string registrationToken)
    {
      await _hubContext.Clients.Group(connectionId).SendAsync("REGISTER_TOKEN_UPDATED", registrationToken).ConfigureAwait(false);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      var token = await _cache.GetStringAsync(Context.ConnectionId).ConfigureAwait(false);
      // remove all keys from cache if exists
      if (!string.IsNullOrEmpty(token))
      {
        await _cache.RemoveAsync(token).ConfigureAwait(false);
        await _cache.RemoveAsync(Context.ConnectionId).ConfigureAwait(false);
      }
      await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    private void GenerateRegistrationToken(string connectionId)
    {
      Generate(connectionId, null);
      var timer = new Timer(TimeSpan.FromMinutes(int.Parse(_configuration[REGISTRATION_TOKEN_INTERVALL])).TotalMilliseconds);
      timer.Elapsed += (sender, args) => Generate(connectionId, timer);
      timer.Start();
    }

    private async void Generate(string connectionId, Timer? timer)
    {
      // because cache is just only a key value store we need to add both token and connection as key,
      // so the validation of token and validation of existing connection can be proceeded.
      // token validation needs to map to one connectionId to inform the admin by this connection for a successful client registration
      var oldToken = await _cache.GetStringAsync(connectionId).ConfigureAwait(false);
      if (timer != null && string.IsNullOrEmpty(oldToken))
      {
        timer.Dispose();
      }

      if (!string.IsNullOrEmpty(oldToken))
      {
        await _cache.RemoveAsync(oldToken).ConfigureAwait(false);
      }

      var registrationToken = new Password(64).IncludeLowercase().IncludeUppercase().IncludeNumeric().Next();
      await _cache.SetStringAsync(connectionId, registrationToken).ConfigureAwait(false);
      await _cache.SetStringAsync(registrationToken, connectionId).ConfigureAwait(false);
      await UpdateRegistrationToken(connectionId, registrationToken).ConfigureAwait(false);
    }
  }
}
