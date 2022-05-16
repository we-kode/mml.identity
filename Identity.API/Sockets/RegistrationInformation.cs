namespace Identity.Sockets
{
  /// <summary>
  /// Contains the information one client needs to be registered
  /// </summary>
  public class RegistrationInformation
  {
    /// <summary>
    /// The actual registration token
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// The app key needed to send request to the api.
    /// </summary>
    public string AppKey { get; set; }

    /// <summary>
    /// Constructs one instance
    /// </summary>
    /// <param name="token">The actual registration token</param>
    /// <param name="appKey">The app key needed to send request to the api</param>
    public RegistrationInformation(string token, string appKey)
    {
      Token = token;
      AppKey = appKey;
    }
  }
}
