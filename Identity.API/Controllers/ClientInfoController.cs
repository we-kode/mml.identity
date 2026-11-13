using Asp.Versioning;
using Identity.Application.Contracts;
using Identity.Application.IdentityConstants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Identity.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/identity/internal/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Scopes.IdentityInternal)]
public class ClientInfoController(IClientRepository clientRepository) : ControllerBase
{

  // TODO: only api service can login

  /// <summary>
  /// Loads groups of the client.
  /// </summary>
  /// <param name="clientId">The client id</param>
  /// <returns>List of group ids.</returns>
  [HttpGet("{clientId}/groups")]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public ActionResult<List<Guid>> GetGroups(string clientId)
  {
    if (!clientRepository.ClientExists(clientId))
    {
      return NotFound(); 
    }

    var dbClient = clientRepository.GetClient(clientId!);
    return dbClient.Groups.Select(g => g.Id).ToList();
  }
}
