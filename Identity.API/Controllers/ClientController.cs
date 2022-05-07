using AutoMapper;
using Identity.Application;
using Identity.Application.Contracts;
using Identity.Application.Models;
using Identity.Application.Services;
using Identity.Contracts;
using Identity.Filters;
using Identity.Sockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Identity.Controllers
{
  [ApiController]
  [ApiVersion("1.0")]
  [Route("api/v{version:apiVersion}/identity/[controller]")]
  [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.ADMIN)]
  public class ClientController : ControllerBase
  {
    private readonly IClientRepository clientRepository;
    private readonly IHubContext<RegisterClientHub> hubContext;
    private readonly ClientApplicationService _service;
    private readonly IMapper _mapper;

    public ClientController(IClientRepository clientRepository, IHubContext<RegisterClientHub> hubContext, ClientApplicationService service, IMapper mapper)
    {
      this.clientRepository = clientRepository;
      this.hubContext = hubContext;
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
    /// <response code="404">If user does not exists.</response>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Post([FromBody] ClientUpdateRequest request)
    {
      if (!clientRepository.ClientExists(request.ClientId.ToString()))
      {
        return NotFound();
      }

      clientRepository.Update(_mapper.Map<Client>(request));
      return Ok();
    }

    /// <summary>
    /// Registers a new client
    /// </summary>
    /// <returns>The new application client</returns>
    /// <response code="400">If update fails or information are invalid.</response>
    /// <response code="403">If invalid token is provided.</response>
    /// <response code="500">If generating client failed.</response>
    [HttpPost("register/{regToken}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    [ServiceFilter(typeof(TokenRegistrationFilter))]
    public async Task<ActionResult<ApplicationClient>> Register([FromBody] RegisterClientRequest request, string regToken, [FromQuery] string? conId)
    {
      try
      {
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(request.Base64PublicKey), out int _);
      }
      catch (CryptographicException)
      {
        return BadRequest();
      }

      var client = await _service.CreateClient(request.Base64PublicKey).ConfigureAwait(false);
      await hubContext.Clients.Group(conId!).SendAsync("CLIENT_REGISTERED", client.ClientId).ConfigureAwait(false);
      return client;
    }
  }
}
