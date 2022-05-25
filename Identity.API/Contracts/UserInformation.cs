using System.ComponentModel.DataAnnotations;

namespace Identity.Contracts
{
  /// <summary>
  /// Contains information of one user.
  /// </summary>toor
  public class UserInformation
  {
    /// <summary>
    /// The actual user name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The old password of the user. 
    /// If password should not be changed, than both <see cref="OldPassword"/> and <see cref="NewPassword"/> should be an empty string.
    /// </summary>
    [MinLength(12, ErrorMessageResourceName = "MinLength", ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
    public string? OldPassword { get; set; }

    /// <summary>
    /// The new Password of the user.
    /// If password should not be changed, than both <see cref="OldPassword"/> and <see cref="NewPassword"/> should be an empty string.
    /// </summary>
    [MinLength(12, ErrorMessageResourceName = "MinLength", ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
    public string? NewPassword { get; set; }

    /// <summary>
    /// Creates user informations
    /// </summary>
    /// <param name="name">The actual user name</param>
    /// <param name="oldPassword">The old password of the user. If password should not be changed, than both <see cref="OldPassword"/> and <see cref="NewPassword"/> should be an empty string.</param>
    /// <param name="newPassword">The new Password of the user. If password should not be changed, than both <see cref="OldPassword"/> and <see cref="NewPassword"/> should be an empty string.</param>
    public UserInformation(string name, string? oldPassword = null, string? newPassword = null)
    {
      Name = name;
      OldPassword = oldPassword;
      NewPassword = newPassword;
    }
  }
}
