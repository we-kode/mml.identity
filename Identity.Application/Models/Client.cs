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
    /// Inits a client
    /// </summary>
    /// <param name="clientId">The id of the client</param>
    /// <param name="displayName">The display name of the client</param>
    public Client(string clientId, string displayName)
    {
      ClientId = clientId;
      DisplayName = displayName;
    }
  }
}
