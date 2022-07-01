using System.ComponentModel.DataAnnotations;

namespace Identity.Contracts
{
  /// <summary>
  /// Group create and update request.
  /// </summary>
  public class GroupRequest
  {
    /// <summary>
    /// The name.
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.ValidationMessages.Required), ErrorMessageResourceType = typeof(Resources.ValidationMessages))]
    public string Name { get; set; }

    /// <summary>
    /// The default flag.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Creates instance.
    /// </summary>
    /// <param name="name">The group name.</param>
    /// <param name="isDefault">The default flag.</param>
    public GroupRequest(string name, bool isDefault)
    {
      Name = name;
      IsDefault = isDefault;
    }
  }
}
