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
  public class RegistrationInformation(string token, string appKey)
  {
    /// <summary>
    /// The actual registration token
    /// </summary>
    public string Token { get; set; } = token;

    /// <summary>
    /// The app key needed to send request to the api.
    /// </summary>
    public string AppKey { get; set; } = appKey;
  }
}
