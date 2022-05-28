using System;
using System.ComponentModel.DataAnnotations;

namespace Identity.Contracts
{
  /// <summary>
  /// Request to update a client display name
  /// </summary>
  public class ClientUpdateRequest
  {
    /// <summary>
    /// The id of the client
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.ValidationMessages.Required), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
    public Guid ClientId { get; set; }

    /// <summary>
    /// The display name of the client
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.ValidationMessages.Required), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
    public string DisplayName { get; set; }

    /// <summary>
    /// Inits a client
    /// </summary>
    /// <param name="clientId">The id of the client</param>
    /// <param name="displayName">The display name of the client</param>
    public ClientUpdateRequest(Guid clientId, string displayName)
    {
      ClientId = clientId;
      DisplayName = displayName;
    }
  }
}
