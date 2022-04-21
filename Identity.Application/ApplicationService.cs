using Identity.Application.Contracts;
using Identity.Application.Models;

namespace Identity.Application
{
  /// <summary>
  /// Handles identity functions.
  /// </summary>
  public class ApplicationService
  {
    private readonly IIdentityRepository _identityRepository;

    public ApplicationService(IIdentityRepository identityRepository)
    {
      _identityRepository = identityRepository;
    }


    /// <summary>
    /// Creates a new user login
    /// </summary>
    /// <param name="userName">Name of the user</param>
    /// <param name="initPassword">The initial password for the created user</param>
    public async Task<User> Create(string userName, string initPassword)
    {
      if (await _identityRepository.UserExists(userName).ConfigureAwait(false))
      {
        throw new ArgumentException($"User with name {nameof(userName)} already exists.");
      }
      var user = await _identityRepository.CreateNewUser(userName, initPassword).ConfigureAwait(false);
      return user;
    }

    /// <summary>
    /// Deletes one user account
    /// </summary>
    /// <param name="actualUserId">The user id who performs the action</param>
    /// <param name="userId">Id of user to be deleted</param>
    public async Task DeleteUser(long actualUserId, long userId)
    {
      if (actualUserId == userId)
      {
        return;
      }

      await _identityRepository.Delete(userId).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates one user account
    /// </summary>
    /// <param name="id">User id</param>
    /// <param name="userName">User new name</param>
    /// <param name="changedPassword">New init password. Null if only name was changed.</param>
    /// <returns></returns>
    public async Task UpdateUser(long id, string userName, string? changedPassword)
    {
      await _identityRepository.UpdateUserName(id, userName);
      if (!string.IsNullOrEmpty(changedPassword))
      {
        await _identityRepository.ResetPassword(id, changedPassword).ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Updates one user account
    /// </summary>
    /// <param name="id">User id</param>
    /// <param name="userName">User new email</param>
    /// <param name="oldPassword">The oldPassword of the user or null, if password was not changed.</param>
    /// <param name="newPassword">The new password of user or null if password was not changed.</param>
    /// <returns>True, if update was successful</returns>
    public async Task<bool> Update(long id, string userName, string? oldPassword = "", string? newPassword = "")
    {
      await _identityRepository.UpdateUserName(id, userName).ConfigureAwait(false);
      if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
      {
        return true;
      }

      return await _identityRepository.UpdateUserPassword(id, oldPassword, newPassword).ConfigureAwait(false);
    }
  }
}
