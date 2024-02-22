using AutoMapper;
using Identity.Application.Contracts;
using Identity.Application.IdentityConstants;
using Identity.Application.Models;
using Identity.Application.Services;
using Identity.Contracts;
using Identity.Filters;
using Identity.Sockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static StackExchange.Redis.Role;

namespace Identity.Controllers
{
  [ApiController]
  [ApiVersion("1.0")]
  [Route("api/v{version:apiVersion}/identity/[controller]")]
  public class ClientController : ControllerBase
  {
    private readonly IClientRepository clientRepository;
    private readonly IHubContext<RegisterClientHub> hubContext;
    private readonly ClientApplicationService _service;
    private readonly IMapper _mapper;

    private readonly IConfiguration _configuration;

    public ClientController(IClientRepository clientRepository,
      IHubContext<RegisterClientHub> hubContext,
      ClientApplicationService service,
      IMapper mapper,
      IConfiguration configuration)
    {
      this.clientRepository = clientRepository;
      this.hubContext = hubContext;
      _service = service;
      _mapper = mapper;
      _configuration = configuration;
    }

    /// <summary>
    /// Loads a list of existing clients.
    /// </summary>
    /// <param name="filter">Filter request to filter the list of clients</param>
    /// <param name="skip">Offset of the list</param>
    /// <param name="take">Size of chunk to be loaded</param>
    /// <returns><see cref="Clients"/></returns>
    [Obsolete("Use POST method instead.")]
    [HttpGet("list")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
    public Clients List([FromQuery] string? filter, [FromQuery] int skip = Application.IdentityConstants.List.Skip, [FromQuery] int take = Application.IdentityConstants.List.Take)
    {
      return clientRepository.ListClients(new Application.Contracts.TagFilter(), filter, skip, take);
    }

    /// <summary>
    /// Loads a list of existing clients.
    /// </summary>
    /// <param name="tagFilter"><see cref="TagFilter"/> to be used to filter clients.</param>
    /// <param name="filter">Filter request to filter the list of clients</param>
    /// <param name="skip">Offset of the list</param>
    /// <param name="take">Size of chunk to be loaded</param>
    /// <returns><see cref="Clients"/></returns>
    [HttpPost("list")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
    public Clients List([FromBody] Contracts.TagFilter tagFilter, [FromQuery] string? filter, [FromQuery] int skip = Application.IdentityConstants.List.Skip, [FromQuery] int take = Application.IdentityConstants.List.Take)
    {
      return clientRepository.ListClients(_mapper.Map<Application.Contracts.TagFilter>(tagFilter), filter, skip, take);
    }

    /// <summary>
    /// Deletes a list of existing clients.
    /// </summary>
    /// <param name="ids">ids of the clients to be removed.</param>
    [HttpPost("deleteList")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
    public IActionResult DeleteList([FromBody] IList<string> ids)
    {
      foreach (var id in ids)
      {
        clientRepository.DeleteClient(id);
      }

      return Ok();
    }

    /// <summary>
    /// Assigns clients to groups.
    /// </summary>
    /// <param name="ids">ids of the clients to be assigned.</param>
    [HttpPost("assign")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
    public IActionResult Assign([FromBody] AssignmentRequest request)
    {
      clientRepository.Assign(request.Items, request.InitGroups, request.Groups);
      return Ok();
    }

    /// <summary>
    /// Loads selected groups
    /// </summary>
    /// <param name="ids">ids of the clients to load groups from.</param>
    [HttpPost("assignedGroups")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
    public Groups AssignedGroups([FromBody] List<string> clients)
    {
      return clientRepository.GetAssignedGroups(clients);
    }

    /// <summary>
    /// Deletes one existing client.
    /// </summary>
    /// <param name="id">id of the client to be removed.</param>
    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
    public IActionResult Delete(string id)
    {
      clientRepository.DeleteClient(id);
      return Ok();
    }

    /// <summary>
    /// Loads one existing client.
    /// </summary>
    /// <param name="id">id of the client to be loaded.</param>
    /// <returns><see cref="User"/> of given id</returns>
    /// <response code="404">If client does not exist.</response>
    [HttpGet("{id:Guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
    public ActionResult<Client> Get(Guid id)
    {
      if (!clientRepository.ClientExists(id.ToString()))
      {
        return NotFound();
      }

      return clientRepository.GetClient(id.ToString());
    }

    /// <summary>
    /// Changes client display name.
    /// </summary>
    /// <param name="request">New Userinformation to store.</param>
    /// <response code="404">If user does not exists.</response>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
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
    /// Returns server connection settings for a client.
    /// </summary>
    [HttpGet("connection_settings")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Admin)]
    public IActionResult GetConnectionSettings()
    {
      return new JsonResult(new
      {
        ApiKey = _configuration.GetValue("APP_KEY", string.Empty)
      });
    }

    /// <summary>
    /// Returns whether the current authenticated client is a registered client.
    /// </summary>
    [HttpGet()]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Client)]
    public IActionResult ClientRegistered()
    {
      var clientId = HttpContext.User.GetClaim(Claims.Subject) ?? "";

      return new JsonResult(new
      {
        Registered = clientRepository.ClientExists(clientId)
      });
    }

    /// <summary>
    /// Deletes the registration of the current authenticated client.
    /// </summary>
    [HttpPost("removeRegistration")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Policy = Roles.Client)]
    public IActionResult RemoveRegistration()
    {
      var clientId = HttpContext.User.GetClaim(Claims.Subject) ?? "";
      clientRepository.DeleteClient(clientId);
      return Ok();
    }

    /// <summary>
    /// Registers a new client
    /// </summary>
    /// <returns>The new application client</returns>
    /// <response code="400">If update fails or information are invalid.</response>
    /// <response code="403">If invalid token is provided.</response>
    /// <response code="HttpStatusCodes.BusinessError">If client creation failed.</response>
    [HttpPost("register/{regToken}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpStatusCodes.BusinessError)]
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

      var client = await _service.CreateClient(request.Base64PublicKey, request.DisplayName, request.DeviceIdentifier).ConfigureAwait(false);
      if (client == null)
      {
        return StatusCode(HttpStatusCodes.BusinessError, "CLIENT_CREATION_FAILED");
      }
      await hubContext.Clients.Group(conId!).SendAsync("CLIENT_REGISTERED", client.ClientId).ConfigureAwait(false);
      return client;
    }
  }
}
