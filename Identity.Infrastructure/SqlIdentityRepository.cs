using Identity.Application.Contracts;
using Identity.Application.IdentityConstants;
using Identity.Application.Models;
using Identity.DBContext;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure
{
  public class SqlIdentityRepository(UserManager<IdentityUser<long>> userManager, Func<ApplicationDBContext> contextFactory) : IIdentityRepository
  {
    private const string ADMIN_ROLE = Roles.Admin;

    public async Task<User> CreateNewUser(string userName, string initPassword)
    {
      var login = new IdentityUser<long>
      {
        UserName = userName,
        NormalizedUserName = userName
      };

      var result = await userManager.CreateAsync(login).ConfigureAwait(false);
      if (!result.Succeeded)
      {
        throw new Exception($"User {userName} exists already");
      }

      await userManager.AddToRoleAsync(login, ADMIN_ROLE).ConfigureAwait(false);
      await userManager.AddPasswordAsync(login, initPassword).ConfigureAwait(false);

      return new User(login.Id, userName, true, login.EmailConfirmed);
    }

    public async Task<bool> UserExists(string userName, long? userId)
    {
      var result = await userManager.FindByNameAsync(userName).ConfigureAwait(false);
      return result != null && (!userId.HasValue || result.Id != userId.Value);
    }

    public Users ListUsers(long actualUserId, string? filter, int skip = List.Skip, int take = List.Take)
    {
      using var context = contextFactory();
      var adminRoleId = context.Roles.Where(role => role.Name == ADMIN_ROLE).FirstOrDefault()?.Id ?? 0;
      var query = context.Users
          .Where(user => string.IsNullOrEmpty(filter) || EF.Functions.ILike(user.UserName ?? string.Empty, $"%{filter}%"))
          .OrderBy(user => user.UserName);

      var count = query.Count();
      var items = query.Select(user => new User(
        user.Id,
        user.UserName ?? string.Empty,
        context.UserRoles.Where(userRole => userRole.UserId == user.Id && userRole.RoleId == adminRoleId).Any(),
        user.EmailConfirmed,
        user.Id != actualUserId
      ))
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
      var result = await userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
      return result != null;
    }

    public async Task<User> GetUser(long id)
    {
      var result = await userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
      var isAdmin = await userManager.IsInRoleAsync(result!, ADMIN_ROLE).ConfigureAwait(false);
      return new User(result!.Id, result.UserName ?? string.Empty, isAdmin, result.EmailConfirmed);
    }

    public async Task Delete(long userId)
    {
      var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
      if (user == null)
      {
        return;
      }
      await userManager.DeleteAsync(user).ConfigureAwait(false);
    }

    public async Task UpdateUserName(long id, string name)
    {
      var user = await userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
      if (user == null || user.UserName == name)
      {
        return;
      }
      user.UserName = name;
      user.NormalizedUserName = name;
      await userManager.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task<bool> UpdateUserPassword(long id, string oldPassword, string newPassword)
    {
      var user = await userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
      if (user == null)
      {
        return false;
      }
      var result = await userManager.ChangePasswordAsync(user, oldPassword, newPassword).ConfigureAwait(false);
      if (result.Succeeded)
      {
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user).ConfigureAwait(false);
      }
      return result.Succeeded;
    }

    public async Task<bool> Validate(string name, string password)
    {
      var user = await userManager.FindByNameAsync(name).ConfigureAwait(false);
      if (user == null)
      {
        return false;
      }
      return await userManager.CheckPasswordAsync(user, password).ConfigureAwait(false);
    }

    public async Task<User> GetUser(string name)
    {
      var result = await userManager.FindByNameAsync(name).ConfigureAwait(false);
      var isAdmin = await userManager.IsInRoleAsync(result!, ADMIN_ROLE).ConfigureAwait(false);
      return new User(result!.Id, result.UserName ?? string.Empty, isAdmin, result.EmailConfirmed);
    }

    public async Task ResetPassword(long id, string changedPassword)
    {
      var user = await userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
      if (user == null)
      {
        return;
      }
      user.EmailConfirmed = false;
      await userManager.UpdateAsync(user).ConfigureAwait(false);
      await userManager.RemovePasswordAsync(user).ConfigureAwait(false);
      await userManager.AddPasswordAsync(user, changedPassword).ConfigureAwait(false);
    }

    public async Task<bool> IsActive(long userId)
    {
      var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
      return user != null && user.EmailConfirmed;
    }
  }
}
