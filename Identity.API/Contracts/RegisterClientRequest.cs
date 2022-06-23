using System.ComponentModel.DataAnnotations;

namespace Identity.Contracts
{

  /// <summary>
  /// Request for registering one client
  /// </summary>
  public class RegisterClientRequest
  {
    /// <summary>
    /// Public Key of the client base64 encoded.
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.ValidationMessages.Required), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
    public string Base64PublicKey { get; set; }

    /// <summary>
    /// The name of the client which will be shown to admins.
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.ValidationMessages.Required), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
    public string DisplayName { get; set; }

    /// <summary>
    /// An identification of the device the client belongs to to differentiate between devices. E.g. the device name.
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.ValidationMessages.Required), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
    public string Device { get; set; }

    /// <summary>
    /// Inits request
    /// </summary>
    /// <param name="base64PublicKey">Public Key of the client base64 encoded</param>
    /// <param name="displayName">The name of the client which will be shown to admins.</param>
    /// <param name="device">An identification of the device the client belongs to to differentiate between devices. E.g. the device name.</param>
    public RegisterClientRequest(string base64PublicKey, string displayName, string device)
    {
      Base64PublicKey = base64PublicKey;
      DisplayName = displayName;
      Device = device;
    }
  }
}
