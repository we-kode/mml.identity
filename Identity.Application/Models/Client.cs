using System;

namespace Identity.Application.Models
{
  /// <summary>
  /// Represents a client in the application
  /// </summary>
  public class Client
  {
    /// <summary>
    /// The id of the client
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// The display name of the client
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// An identification of the device the client belongs to, to differentiate between devices. E.g. the device name.
    /// </summary>
    public string DeviceIdentifier { get; }

    /// <summary>
    /// The last date and time the client requested a new token.
    /// </summary>
    public DateTime LastTokenRefreshDate { get; set; }

    /// <summary>
    /// Inits a client
    /// </summary>
    /// <param name="clientId">The id of the client</param>
    /// <param name="displayName">The display name of the client</param>
    /// <param name="lastTokenRefreshDate">The last date and time the client requested a new token</param>
    /// <param name="deviceIdentifier">An identification of the device the client belongs to, to differentiate between devices. E.g. the device name.</param>
    public Client(string clientId, string displayName, string deviceIdentifier)
    {
      ClientId = clientId;
      DisplayName = displayName;
      DeviceIdentifier = deviceIdentifier;
    }
  }
}
