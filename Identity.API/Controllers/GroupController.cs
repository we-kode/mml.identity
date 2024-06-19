using Asp.Versioning;
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
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Identity.Controllers
{
  [ApiController]
  [ApiVersion("1.0")]
  [Route("api/v{version:apiVersion}/identity/[controller]")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
  public class GroupController : ControllerBase
  {
    private IGroupRepository _repository;
    private IStringLocalizer<ValidationMessages> _localizer;

    public GroupController(
      IGroupRepository repository,
      IStringLocalizer<ValidationMessages> localizer
    )
    {
      _repository = repository;
      _localizer = localizer;
    }

    /// <summary>
    /// Loads a list of existing groups.
    /// </summary>
    /// <param name="request">Filter request to filter the list of groups.</param>
    /// <param name="skip">Offset of the list.</param>
    /// <param name="take">Size of chunk to be loaded.</param>
    /// <returns><see cref="Groups"/></returns>
    [HttpGet()]
    public ActionResult<Groups> List([FromQuery] string? filter, [FromQuery] int skip = Application.IdentityConstants.List.Skip, [FromQuery] int take = Application.IdentityConstants.List.Take)
    {
      return _repository.ListGroups(filter, skip, take);
    }

    /// <summary>
    /// Loads one existing group.
    /// </summary>
    /// <param name="id">Id of the group to be loaded.</param>
    /// <returns><see cref="Group"/> of given id.</returns>
    /// <response code="404">If group does not exist.</response>
    [HttpGet("{id:Guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ServiceFilter(typeof(GroupExistsFilter))]
    public async Task<ActionResult<Group>> Get(Guid id)
    {
      return await _repository.GetGroup(id).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates one group.
    /// </summary>
    /// <param name="request">Group to be created.</param>
    /// <response code="400">If group already exists.</response>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] GroupRequest request)
    {
      if (await _repository.GroupExists(request.Name).ConfigureAwait(false))
      {
        ModelState.AddModelError(
          nameof(GroupRequest.Name),
          _localizer[nameof(ValidationMessages.Unique)]
        );
      }

      if (!ModelState.IsValid)
      {
        return ValidationProblem();
      }

      var group = await _repository
        .CreateNewGroup(request.Name, request.IsDefault)
        .ConfigureAwait(false);
      return Created($"/group/{group.Id}", group);
    }

    /// <summary>
    /// Updates a group.
    /// </summary>
    /// <param name="id">The id of group to be changed.</param>
    /// <param name="request">New information to store.</param>
    [HttpPost("{id:Guid}")]
    [ServiceFilter(typeof(GroupExistsFilter))]
    public async Task<IActionResult> Post(Guid id, [FromBody] GroupRequest request)
    {
      if (await _repository.GroupExists(request.Name, id).ConfigureAwait(false))
      {
        ModelState.AddModelError(
          nameof(GroupRequest.Name),
          _localizer[nameof(ValidationMessages.Unique)]
        );
      }

      if (!ModelState.IsValid)
      {
        return ValidationProblem();
      }

      await _repository
        .UpdateGroup(new Group(id, request.Name, request.IsDefault))
        .ConfigureAwait(false);
      return Ok();
    }

    /// <summary>
    /// Deletes a list of existing groups.
    /// </summary>
    /// <param name="ids">Ids of the groups to be removed.</param>
    [HttpPost("deleteList")]
    public async Task<IActionResult> DeleteList([FromBody] ICollection<Guid> ids)
    {
      foreach (var id in ids)
      {
        await _repository.DeleteGroup(id).ConfigureAwait(false);
      }

      return Ok();
    }
  }
}
