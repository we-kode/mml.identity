using OpenIddict.Abstractions;
using System.Collections.Generic;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Extensions
{
  public static class ClaimExtensions
  {
    public static IEnumerable<string> GetDestinations(this Claim claim, ClaimsPrincipal principal)
    {
      switch (claim.Type)
      {
        case Claims.Name:
          yield return Destinations.AccessToken;

          if (principal.HasScope(Scopes.Profile))
            yield return Destinations.IdentityToken;

          yield break;

        case Claims.Email:
          yield return Destinations.AccessToken;

          if (principal.HasScope(Scopes.Email))
            yield return Destinations.IdentityToken;

          yield break;

        case Claims.Role:
          yield return Destinations.AccessToken;

          if (principal.HasScope(Scopes.Roles))
            yield return Destinations.IdentityToken;

          yield break;

        // Never include the security stamp in the access and identity tokens, as it's a secret value.
        case "AspNet.Identity.SecurityStamp": yield break;

        default:
          yield return Destinations.AccessToken;
          yield break;
      }
    }
  }
}
