using Identity.Application;
using Identity.Application.Contracts;
using Identity.Application.IdentityConstants;
using Identity.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Controllers
{
  [Route("/api/v{version:apiVersion}/identity/connect")]
  [ApiController]
  public class AuthenticationController : Controller
  {
    private readonly IIdentityRepository _identityRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ApplicationService _applicationService;

    public AuthenticationController(IIdentityRepository identityRepository, IClientRepository clientRepository, ApplicationService applicationService)
    {
      _identityRepository = identityRepository;
      _clientRepository = clientRepository;
      _applicationService = applicationService;
    }

    /// <summary>
    /// Handles the authentication attempts of one user
    /// </summary>
    /// <returns>The bearer token on successful signin.</returns>
    /// <response code="401">If authorization fails.</response>
    [HttpPost("token")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Exchange()
    {
      var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException();
      if (request.IsPasswordGrantType())
      {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
          return Unauthorized();
        }

        // login user and return token
        if (!await _identityRepository.Validate(request.Username, request.Password))
        {
          return Unauthorized();
        }

        var user = await _identityRepository.GetUser(request.Username).ConfigureAwait(false);

        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            Claims.Name,
            Claims.Role);

        identity.AddClaim(Claims.Subject, user.Id.ToString(), Destinations.AccessToken);
        identity.AddClaim(Claims.Name, user.Name, Destinations.AccessToken);
        if (user.IsAdmin)
        {
          identity.AddClaim(Claims.Role, Roles.Admin, Destinations.AccessToken);
        }
        var claimsPrincipal = new ClaimsPrincipal(identity);
        claimsPrincipal.SetScopes(request.GetScopes());
        claimsPrincipal.SetResources("resource_server_1");

        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
      }

      if (request.IsRefreshTokenGrantType())
      {
        // return refreshed token, if refresh token is valid. else unauthorized
        var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal
                ?? throw new InvalidOperationException();
        var userId = principal.GetClaim(Claims.Subject)
                ?? throw new InvalidOperationException();

        if (!await _identityRepository.UserExists(long.Parse(userId)).ConfigureAwait(false))
        {
          return Unauthorized();
        }

        if (!await _identityRepository.IsActive(long.Parse(userId)).ConfigureAwait(false))
        {
          return Unauthorized();
        }

        foreach (var claim in principal.Claims)
        {
          claim.SetDestinations(claim.GetDestinations(principal));
        }

        // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
      }

      if (request.IsClientCredentialsGrantType())
      {

        var client = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, null, Claims.Role);
        client.AddClaim(Claims.Subject, request.ClientId!, Destinations.AccessToken);

        if (string.IsNullOrEmpty(request.CodeChallenge))
        {
          return Unauthorized();
        }

        // check if signature is valid with public key saved for client id
        var pubKeyStringB64 = _clientRepository.GetPublicKey(request.ClientId!);
        if (string.IsNullOrEmpty(pubKeyStringB64))
        {
          return Unauthorized();
        }

        var pubKey = Convert.FromBase64String(pubKeyStringB64);
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(pubKey, out int _);
        /*
         * signature must be made over the following string to be marked as valid
         * { "clientId" : "<id of client>", "clientSecret" : "<secret of client>", "grant_type" : "client_credentials" }
         */
        var content = $"{{\"grant_type\":\"client_credentials\",\"client_id\":\"{request.ClientId}\",\"client_secret\":\"{request.ClientSecret}\"}}";
        var isValidSignature = rsa!.VerifyData(Encoding.UTF8.GetBytes(content), Convert.FromBase64String(request.CodeChallenge!), HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
        if (!isValidSignature)
        {
          return Unauthorized();
        }

        client.AddClaim(Claims.Role, Roles.Client, Destinations.AccessToken);
        var claimsPrincipal = new ClaimsPrincipal(client);
        claimsPrincipal.SetScopes(request.GetScopes());
        claimsPrincipal.SetResources("resource_server_1");

        _clientRepository.UpdateTokenRequestDate(request.ClientId!);
        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
      }

      return Unauthorized();
    }

    /// <summary>
    /// Returns user info
    /// </summary>
    /// <returns><see cref="User"/></returns>
    [HttpGet("userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
    public async Task<IActionResult> UserInfo()
    {
      var userIdClaim = HttpContext.User.GetClaim(Claims.Subject);
      if (!long.TryParse(userIdClaim, out var userId) || !await _identityRepository.UserExists(userId))
      {
        return BadRequest();
      }

      var user = await _identityRepository.GetUser(userId).ConfigureAwait(false);
      return Ok(user);
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    /// <returns></returns>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
    public async Task<IActionResult> Logout()
    {
      var userIdClaim = HttpContext.User.GetClaim(Claims.Subject);
      if (!long.TryParse(userIdClaim, out var userId) || !await _identityRepository.UserExists(userId))
      {
        return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
      }

      await _applicationService.RevokeTokens(userId).ConfigureAwait(false);

      return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
  }
}
