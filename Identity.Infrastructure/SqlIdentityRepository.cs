using Identity.Application.Contracts;
using Identity.Application.IdentityConstants;
using Identity.Application.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.Infrastructure
{
  public class SqlIdentityRepository : IIdentityRepository
  {
    private readonly UserManager<IdentityUser<long>> _userManager;
    private const string ADMIN_ROLE = Roles.Admin;

    public SqlIdentityRepository(UserManager<IdentityUser<long>> userManager)
    {
      _userManager = userManager;
    }

    public async Task<User> CreateNewUser(string userName, string initPassword)
    {
      var login = new IdentityUser<long>
      {
        UserName = userName,
        NormalizedUserName = userName
      };

      var result = await _userManager.CreateAsync(login).ConfigureAwait(false);
      if (!result.Succeeded)
      {
        throw new Exception($"User {userName} exists already");
      }

      await _userManager.AddToRoleAsync(login, ADMIN_ROLE).ConfigureAwait(false);
      await _userManager.AddPasswordAsync(login, initPassword).ConfigureAwait(false);

      return new User(login.Id, userName, true, login.EmailConfirmed);
    }

    public async Task<bool> UserExists(string userName)
    {
      var result = await _userManager.FindByNameAsync(userName).ConfigureAwait(false);
      return result != null;
    }

    public Users ListUsers(long actualUserId, string? filter, int skip = List.Skip, int take = List.Take)
    {
      var query = _userManager.Users
          .Where(user => string.IsNullOrEmpty(filter) || user.UserName.Contains(filter, StringComparison.OrdinalIgnoreCase))
          .OrderBy(user => user.UserName);

      var count = query.Count();
      var items = query.Select(user => new User(user.Id, user.UserName, _userManager.IsInRoleAsync(user, ADMIN_ROLE).Result, user.EmailConfirmed, user.Id != actualUserId))
          .Skip(skip)
          .Take(take)
          .ToList();
      return new Users
      {
        TotalCount = count,
        Items = items
      };
    }

    public async Task<bool> UserExists(long id)
    {
      var result = await _userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
      return result != null;
    }

    public async Task<User> GetUser(long id)
    {
      var result = await _userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
      var isAdmin = await _userManager.IsInRoleAsync(result, ADMIN_ROLE).ConfigureAwait(false);
      return new User(result.Id, result.UserName, isAdmin, result.EmailConfirmed);
    }

    public async Task Delete(long userId)
    {
      var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
      await _userManager.DeleteAsync(user).ConfigureAwait(false);
    }

    public async Task UpdateUserName(long id, string name)
    {
      var user = await _userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
      if (user.UserName == name)
      {
        return;
      }
      user.UserName = name;
      user.NormalizedUserName = name;
      await _userManager.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task<bool> UpdateUserPassword(long id, string oldPassword, string newPassword)
    {
      var user = await _userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
      var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword).ConfigureAwait(false);
      if (result.Succeeded)
      {
        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user).ConfigureAwait(false);
      }
      return result.Succeeded;
    }

    public async Task<bool> Validate(string name, string password)
    {
      var user = await _userManager.FindByNameAsync(name).ConfigureAwait(false);
      return await _userManager.CheckPasswordAsync(user, password).ConfigureAwait(false);
    }

    public async Task<User> GetUser(string name)
    {
      var result = await _userManager.FindByNameAsync(name).ConfigureAwait(false);
      var isAdmin = await _userManager.IsInRoleAsync(result, ADMIN_ROLE).ConfigureAwait(false);
      return new User(result.Id, result.UserName, isAdmin, result.EmailConfirmed);
    }

    public async Task ResetPassword(long id, string changedPassword)
    {
      var user = await _userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
      user.EmailConfirmed = false;
      await _userManager.UpdateAsync(user).ConfigureAwait(false);
      await _userManager.RemovePasswordAsync(user).ConfigureAwait(false);
      await _userManager.AddPasswordAsync(user, changedPassword).ConfigureAwait(false);
    }

    public async Task<bool> IsActive(long userId)
    {
      var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
      return user != null && user.EmailConfirmed;
    }
  }
}
