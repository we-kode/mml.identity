namespace Identity.Application.Models
{
  /// <summary>
  /// Represents an application client, which is just created.
  /// </summary>
  public class ApplicationClient
  {
    //The host address of the client
    public string? Host { get; set; }

    /// <summary>
    /// the client id
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// client secret
    /// </summary>
    public string ClientSecret { get; set; }

    /// <summary>
    /// Inits client
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    public ApplicationClient(string clientId, string clientSecret)
    {
      ClientId = clientId;
      ClientSecret = clientSecret;
    }
  }
}
