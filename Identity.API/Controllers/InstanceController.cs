using Asp.Versioning;
using Identity.Application.IdentityConstants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System;

namespace Identity.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/identity/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
public class InstanceController : ControllerBase
{

  /// <summary>
  /// Loads the instance name of the system.
  /// </summary>
  /// <returns>Instance name of the system.</returns>
  [HttpGet()]
  public ActionResult<string?> Get()
  {
    return Environment.GetEnvironmentVariable("INSTANCE");
  }
}
