using Identity.Application;
using Identity.Application.Contracts;
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
using System.Collections.Generic;
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

    public AuthenticationController(IIdentityRepository identityRepository, IClientRepository clientRepository)
    {
      _identityRepository = identityRepository;
      _clientRepository = clientRepository;
    }

    /// <summary>
    /// Handles the authentication attempts of one user
    /// </summary>
    /// <returns>The bearer token on successful signin.</returns>
    /// <response code="401">If authorization fails.</response>
    /// <response code="400">If grant type is not supported.</response>
    /// <response code="403">If token is invalid or user is not active.</response>
    [HttpPost("token")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Exchange()
    {
      var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
      if (request.IsPasswordGrantType())
      {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
          return BadRequest();
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
          identity.AddClaim(Claims.Role, IdentityConstants.Roles.Admin, Destinations.AccessToken);
        }
        var claimsPrincipal = new ClaimsPrincipal(identity);
        claimsPrincipal.SetScopes(request.GetScopes());

        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
      }

      if (request.IsRefreshTokenGrantType())
      {
        // return refreshed token, if refresh token is valid. else unauthorized
        var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal
                ?? throw new InvalidOperationException("Principal could not be retrived from request");
        var userId = principal.GetClaim(Claims.Subject)
                ?? throw new InvalidOperationException("The Subject could not retrieved from token");

        if (!await _identityRepository.UserExists(long.Parse(userId)).ConfigureAwait(false))
        {
          return Forbid(
              authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
              properties: new AuthenticationProperties(new Dictionary<string, string?>
              {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
              }));
        }

        if (!await _identityRepository.IsActive(long.Parse(userId)).ConfigureAwait(false))
        {
          return Forbid(
              authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
              properties: new AuthenticationProperties(new Dictionary<string, string?>
              {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
              }));
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

        var client = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        client.AddClaim(Claims.Subject, request.ClientId!, Destinations.AccessToken);

        if (request.GetScopes().Contains(IdentityConstants.Scopes.Upload))
        {
          var cp = new ClaimsPrincipal(client).SetScopes(request.GetScopes());
          return SignIn(cp, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

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
        var content = $"{{ \"clientId\" : \"{request.ClientId}\", \"clientSecret\" : \"{request.ClientSecret}\", \"grant_type\" : \"client_credentials\" }}";
        var isValidSignature = rsa!.VerifyData(Encoding.UTF8.GetBytes(content), Convert.FromBase64String(request.CodeChallenge!), HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
        if (!isValidSignature)
        {
          return Unauthorized();
        }

        client.AddClaim(Claims.Role, IdentityConstants.Roles.Client, Destinations.AccessToken);
        var claimsPrincipal = new ClaimsPrincipal(client);
        claimsPrincipal.SetScopes(request.GetScopes());

        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
      }

      return BadRequest(new OpenIddictResponse
      {
        Error = Errors.UnsupportedGrantType
      });
    }

    /// <summary>
    /// Returns user info
    /// </summary>
    /// <returns><see cref="User"/></returns>
    [HttpGet("userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = IdentityConstants.Roles.Admin)]
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
  }
}
