namespace Identity.Application.Models
{
  /// <summary>
  /// Represents one user in the application.
  /// </summary>
  public class User
  {
    /// <summary>
    /// Id of the user
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// The username
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Determines if this user is an admin
    /// </summary>
    public bool IsAdmin { get; }

    /// <summary>
    /// Determines if user has confirmed account
    /// </summary>
    public bool IsConfirmed { get; }

    /// <summary>
    /// Creates an instance of one user.
    /// </summary>
    /// <param name="id">Id of the user</param>
    /// <param name="name">The username</param>
    /// <param name="isAdmin">Determines if this user is an admin</param>
    /// <param name="isConfirmed">Determines if user has confirmed account</param>
    public User(long id, string name, bool isAdmin, bool isConfirmed)
    {
      Id = id;
      Name = name;
      IsAdmin = isAdmin;
      IsConfirmed = isConfirmed;
    }
  }
}
