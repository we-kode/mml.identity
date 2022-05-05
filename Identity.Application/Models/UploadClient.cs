using PasswordGenerator;
using System;

namespace Identity.Application.Models
{
  /// <summary>
  /// Represents an created upload client
  /// </summary>
  public class UploadClient
  {
    /// <summary>
    /// The ClientId
    /// </summary>
    public readonly string ClientId;

    /// <summary>
    /// The client secret
    /// </summary>
    public readonly string ClientSecret;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="clientId">The ClientId</param>
    /// <param name="clientSecret">The client secret</param>
    public UploadClient(string clientId, string clientSecret)
    {
      ClientId = clientId;
      ClientSecret = clientSecret;
    }

    public UploadClient()
    {
      ClientId = Guid.NewGuid().ToString();
      ClientSecret = new Password(101).Next();
    }

    public override string ToString()
    {
      return $"{{ \"client_id\": \"{ClientId}\", \"client_secret\": \"{ClientSecret}\", \"scope\": \"{IdentityConstants.Scopes.Upload}\"}}";
    }
  }
}
