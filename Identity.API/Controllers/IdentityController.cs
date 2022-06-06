using Identity.Application;
using Identity.Application.Contracts;
using Identity.Application.IdentityConstants;
using Identity.Application.Models;
using Identity.Contracts;
using Identity.Filters;
using Identity.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Identity.Controllers
{
  [ApiController]
  [ApiVersion("1.0")]
  [Route("api/v{version:apiVersion}/[controller]/user")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public class IdentityController : ControllerBase
  {
    private ApplicationService _service;
    private IIdentityRepository _repository;
    private IStringLocalizer<ValidationMessages> _localizer;

    public IdentityController(
      ApplicationService service,
      IIdentityRepository repository,
      IStringLocalizer<ValidationMessages> localizer)
    {
      _service = service;
      _repository = repository;
      _localizer = localizer;
    }

    /// <summary>
    /// Loads a list of existing users.
    /// </summary>
    /// <param name="request">Filter request to filter the list pof users</param>
    /// <param name="skip">Offset of the list</param>
    /// <param name="take">Size of chunk to be loaded</param>
    /// <returns><see cref="Users"/></returns>
    /// <response code="404">If user does not exists</response>
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("list")]
    public ActionResult<Users> List([FromQuery] string? filter, [FromQuery] int skip = Application.IdentityConstants.List.Skip, [FromQuery] int take = Application.IdentityConstants.List.Take)
    {
      var id = HttpContext.User.GetClaim(OpenIddictConstants.Claims.Subject);
      if (string.IsNullOrEmpty(id))
      {
        return NotFound();
      }
      return _repository.ListUsers(long.Parse(id), filter, skip, take);
    }

    /// <summary>
    /// Loads one existing user.
    /// </summary>
    /// <param name="id">id of the user to be loaded.</param>
    /// <returns><see cref="User"/> of given id</returns>
    /// <response code="404">If user does not exist.</response>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ServiceFilter(typeof(UserExistsFilter))]
    public async Task<ActionResult<User>> Get(long id)
    {
      return await _repository.GetUser(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes one existing user.
    /// </summary>
    /// <param name="id">id of the user to be removed.</param>
    /// <response code="404">If user does not exists</response>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ServiceFilter(typeof(UserExistsFilter))]
    public async Task<IActionResult> Delete(long id)
    {
      var actualUserId = HttpContext.User.GetClaim(OpenIddictConstants.Claims.Subject);
      if (string.IsNullOrEmpty(actualUserId))
      {
        return NotFound();
      }
      await _service.DeleteUser(long.Parse(actualUserId), id).ConfigureAwait(false);
      return Ok();
    }

    /// <summary>
    /// Deletes a list of existing users.
    /// </summary>
    /// <param name="ids">ids of the users to be removed.</param>
    /// <response code="404">If user does not exists</response>
    [HttpPost("deleteList")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteList([FromBody] IList<long> ids)
    {
      var actualUserId = HttpContext.User.GetClaim(OpenIddictConstants.Claims.Subject);
      if (string.IsNullOrEmpty(actualUserId))
      {
        return NotFound();
      }
      foreach (var id in ids)
      {
        await _service.DeleteUser(long.Parse(actualUserId), id).ConfigureAwait(false);
      }

      return Ok();
    }

    /// <summary>
    /// Creates one user.
    /// </summary>
    /// <param name="request">User to be created.</param>
    /// <response code="400">If user exists already or the username is not a valid email.</response>
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] UserCreationRequest request)
    {
      if (await _repository.UserExists(request.Name).ConfigureAwait(false))
      {
        ModelState.AddModelError(
          nameof(UserCreationRequest.Name),
          _localizer[nameof(ValidationMessages.Unique)]
        );
      }

      if (string.IsNullOrEmpty(request.Password))
      {
        ModelState.AddModelError(
          nameof(UserCreationRequest.Password),
          _localizer[nameof(ValidationMessages.Empty)]
        );
      }

      if (!ModelState.IsValid)
      {
        return ValidationProblem();
      }

      var user = await _service.Create(request.Name, request.Password!).ConfigureAwait(false);
      return Created($"/user/{user.Id}", user);
    }

    /// <summary>
    /// Updates user
    /// </summary>
    /// <param name="id">The id of user to be changed</param>
    /// <param name="request">New information to store.</param>
    [HttpPost("{id:long}")]
    [ServiceFilter(typeof(UserExistsFilter))]
    public async Task<IActionResult> Post(long id, [FromBody] UserCreationRequest request)
    {
      if (await _repository.UserExists(request.Name, id).ConfigureAwait(false))
      {
        ModelState.AddModelError(
          nameof(UserCreationRequest.Name),
          _localizer[nameof(ValidationMessages.Unique)]
        );
      }

      if (!ModelState.IsValid)
      {
        return ValidationProblem();
      }

      await _service.UpdateUser(id, request.Name, request.Password).ConfigureAwait(false);
      return Ok();
    }

    /// <summary>
    /// Change own userinformation.
    /// </summary>
    /// <param name="request">New Userinformation to store.</param>
    /// <response code="400">If user does not exists already or user information are invalid.</response>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] UserInformation request)
    {
      var id = HttpContext.User.GetClaim(OpenIddictConstants.Claims.Subject);
      if (string.IsNullOrEmpty(id))
      {
        return BadRequest("INVALID_ID");
      }
      var isUpdated = await _service.Update(long.Parse(id), request.Name, request.OldPassword, request.NewPassword).ConfigureAwait(false);
      if (!isUpdated)
      {
        return BadRequest("USER_UPDATE_FAILED");
      }
      return Ok();
    }
  }
}
