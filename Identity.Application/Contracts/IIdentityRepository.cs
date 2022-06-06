using Identity.Application.IdentityConstants;
using Identity.Application.Models;
using System.Threading.Tasks;

namespace Identity.Application.Contracts
{
  public interface IIdentityRepository
  {
    /// <summary>
    /// Determines if one user with given email exists
    /// </summary>
    /// <param name="email">Email of user to be checked</param>
    /// <param name="userId">Id of user to ignore</param>
    /// <returns>True, if user with given email already exists</returns>
    Task<bool> UserExists(string email, long? userId = null);

    /// <summary>
    /// Determines if one user with given user id
    /// </summary>
    /// <param name="id">Id of user to be checked</param>
    /// <returns>True, if user with given id already exists</returns>
    Task<bool> UserExists(long id);

    /// <summary>
    /// Creates a new user login
    /// </summary>
    /// <param name="userName">Name of the user</param>
    /// <param name="initPassword">Initial password of the user</param>
    /// <returns>The cerated <see cref="User"/></returns>
    Task<User> CreateNewUser(string userName, string initPassword);

    /// <summary>
    /// Loads list of users.
    /// </summary>
    /// <param name="actualUserId">The userid of the actual user</param>
    /// <param name="filter">Users will be filtered by given string</param>
    /// <param name="skip">Offset of the list</param>
    /// <param name="take">Size of chunk to be loaded</param>
    /// <returns><see cref="Users"/></returns>
    Users ListUsers(long actualUserId, string? filter, int skip = List.Skip, int take = List.Take);

    /// <summary>
    /// Removes one user
    /// </summary>
    /// <param name="userId">Id of user to be deleted</param>
    Task Delete(long userId);

    /// <summary>
    /// Resets a user password to a new init password and sets account as unconfirmed.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="changedPassword"></param>
    /// <returns></returns>
    Task ResetPassword(long id, string changedPassword);

    /// <summary>
    /// Updates the username
    /// </summary>
    /// <param name="id">user id</param>
    /// <param name="name">new user email</param>
    Task UpdateUserName(long id, string name);

    /// <summary>
    /// Updates the userpassword
    /// </summary>
    /// <param name="id">user id</param>
    /// <param name="oldPassword">the old password of the user</param>
    /// <param name="newPassword">the new password of the user</param>
    /// <returns>True, if password could be updated successful</returns>
    Task<bool> UpdateUserPassword(long id, string oldPassword, string newPassword);

    /// <summary>
    /// Loads information of one user
    /// </summary>
    /// <param name="id">User id</param>
    /// <returns><see cref="User"/></returns>
    Task<User> GetUser(long id);

    /// <summary>
    /// Loads information of one user
    /// </summary>
    /// <param name="name">User name</param>
    /// <returns><see cref="User"/></returns>
    Task<User> GetUser(string name);

    /// <summary>
    /// Validates a login attempt of one user.
    /// </summary>
    /// <param name="name">Username</param>
    /// <param name="password">User password</param>
    /// <returns>True, if user credentials are valid.</returns>
    Task<bool> Validate(string name, string password);

    /// <summary>
    /// Checks if user is active
    /// </summary>
    /// <param name="userId">Id to be checked</param>
    /// <returns>True, if user is active</returns>
    Task<bool> IsActive(long userId);
  }
}
