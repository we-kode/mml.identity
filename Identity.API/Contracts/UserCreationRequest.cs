using System.ComponentModel.DataAnnotations;

namespace Identity.Contracts
{
  /// <summary>
  /// Auhtorization information of one user.
  /// </summary>
  public class UserCreationRequest
  {
    /// <summary>
    /// The username
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.ValidationMessages.Required), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
    public string Name { get; set; }

    /// <summary>
    /// the password
    /// </summary>
    [MinLength(12, ErrorMessageResourceName = nameof(Resources.ValidationMessages.MinLength), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
    public string? Password { get; set; }

    /// <summary>
    /// Creates instance
    /// </summary>
    /// <param name="name">The username</param>
    /// <param name="password">The password</param>
    public UserCreationRequest(string name, string? password = null)
    {
      Name = name;
      Password = password;
    }
  }
}
