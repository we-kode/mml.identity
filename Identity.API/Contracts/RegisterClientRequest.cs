using System.ComponentModel.DataAnnotations;

namespace Identity.Contracts
{

  /// <summary>
  /// Request for registering one client
  /// </summary>
  public class RegisterClientRequest
  {
    /// <summary>
    /// Public Key of the client base64 encoded
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.ValidationMessages.Required), ErrorMessageResourceType = typeof(Resources.ValidationMessages)))]
    public string Base64PublicKey { get; set; }

    /// <summary>
    /// Inits request
    /// </summary>
    /// <param name="base64PublicKey">Public Key of the client base64 encoded</param>
    public RegisterClientRequest(string base64PublicKey)
    {
      Base64PublicKey = base64PublicKey;
    }
  }
}
