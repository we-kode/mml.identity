using AutoMapper;
using Identity.Application;
using Identity.Application.Contracts;
using Identity.Application.Models;
using Identity.Application.Services;
using Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System.Collections.Generic;

namespace Identity.Controllers
{
  [ApiController]
  [ApiVersion("1.0")]
  [Route("api/v{version:apiVersion}/identity/[controller]")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.ADMIN)]
  public class ClientController : ControllerBase
  {
    private readonly IClientRepository clientRepository;
    private readonly ClientApplicationService _service;
    private readonly IMapper _mapper;

    public ClientController(IClientRepository clientRepository, ClientApplicationService service, IMapper mapper)
    {
      this.clientRepository = clientRepository;
      _service = service;
      _mapper = mapper;
    }

    /// <summary>
    /// Loads a list of existing clients.
    /// </summary>
    /// <param name="request">Filter request to filter the list pof users</param>
    /// <returns><see cref="IList{T}"/> of <see cref="Client"/></returns>
    [HttpGet("list")]
    public IList<Client> List([FromQuery] string? filter)
    {
      return clientRepository.ListClients(filter);
    }

    /// <summary>
    /// Deletes one existing user.
    /// </summary>
    /// <param name="id">id of the user to be removed.</param>
    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
      clientRepository.DeleteClient(id);
      return Ok();
    }

    /// <summary>
    /// Changes client display name.
    /// </summary>
    /// <param name="request">New Userinformation to store.</param>
    /// <response code="400">If update fails or information are invalid.</response>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] ClientUpdateRequest request)
    {
      var isUpdated = clientRepository.Update(_mapper.Map<Client>(request));
      if (!isUpdated)
      {
        return BadRequest();
      }
      return Ok();
    }

    /// <summary>
    /// Registers a new client
    /// </summary>
    /// <returns>The new application client</returns>
    /// <response code="400">If update fails or information are invalid.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    // check token middleware
    public ApplicationClient Register([FromBody] RegisterClientRequest request)
    {
      var client = _service.CreateClient(request.Base64PublicKey);
      return client;
    }

  }
}
