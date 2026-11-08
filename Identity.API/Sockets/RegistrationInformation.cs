namespace Identity.Sockets
{
  /// <summary>
  /// Contains the information one client needs to be registered
  /// </summary>
  /// <remarks>
  /// Constructs one instance
  /// </remarks>
  /// <param name="token">The actual registration token</param>
  /// <param name="appKey">The app key needed to send request to the api</param>
  /// <param name="serverName">The server name clients should connect to or null, if not configured</param>
  public class RegistrationInformation(string token, string appKey, string? serverName = null)
  {
    /// <summary>
    /// The actual registration token
    /// </summary>
    public string Token { get; set; } = token;

    /// <summary>
    /// The app key needed to send request to the api.
    /// </summary>
    public string AppKey { get; set; } = appKey;

    /// <summary>
    /// The server name in the format host[:port] clients should connect to or null, if not configured.
    /// </summary>
    public string? ServerName { get; set; } = serverName;
  }
}
