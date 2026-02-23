namespace Identity.Application.Models
{
  /// <summary>
  /// Represents one user in the application.
  /// </summary>
  /// <remarks>
  /// Creates an instance of one user.
  /// </remarks>
  /// <param name="id">Id of the user</param>
  /// <param name="name">The username</param>
  /// <param name="isAdmin">Determines if this user is an admin</param>
  /// <param name="isConfirmed">Determines if user has confirmed account</param>
  /// <param name="isDeletable">Determines if user can be deleted</param>
  public class User(long id, string name, bool isAdmin, bool isConfirmed, bool isDeletable = true)
  {
    /// <summary>
    /// Id of the user
    /// </summary>
    public long Id { get; } = id;

    /// <summary>
    /// The username
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Determines if this user is an admin
    /// </summary>
    public bool IsAdmin { get; } = isAdmin;

    /// <summary>
    /// Determines if user has confirmed account
    /// </summary>
    public bool IsConfirmed { get; } = isConfirmed;

    /// <summary>
    /// Determines if user can be deleted
    /// </summary>
    public bool IsDeletable { get; } = isDeletable;
  }
}
