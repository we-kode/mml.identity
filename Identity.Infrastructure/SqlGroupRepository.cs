
using Identity.Application.Contracts;
using Identity.Application.Models;
using Identity.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using DbGroup = Identity.DBContext.Models.Group;
using Rebus.Bus;
using Messages.Events;

namespace Identity.Infrastructure
{
  public class SqlGroupRepository(
    Func<ApplicationDBContext> contextFactory,
    IBus publishEndpoint
    ) : IGroupRepository
  {
    public async Task<Group> CreateNewGroup(string name, bool isDefault)
    {
      using var context = contextFactory();
      var groupId = Guid.NewGuid();
      var group = new DbGroup
      {
        Id = groupId,
        Name = name,
        IsDefault = isDefault
      };

      await context.Groups.AddAsync(group).ConfigureAwait(false);
      await context.SaveChangesAsync().ConfigureAwait(false);

      await publishEndpoint.Publish(new GroupCreated(
        groupId,
        name,
        isDefault
      ))
      .ConfigureAwait(false);

      return new(group.Id, group.Name, group.IsDefault);
    }

    public async Task DeleteGroup(Guid groupId)
    {
      using var context = contextFactory();

      var group = await context.Groups
        .FirstOrDefaultAsync(group => group.Id == groupId)
        .ConfigureAwait(false);

      if (group == null)
      {
        return;
      }

      context.Groups.Remove(group);
      await context.SaveChangesAsync().ConfigureAwait(false);

      await publishEndpoint.Publish(new GroupDeleted(
        groupId
      )).ConfigureAwait(false);
    }

    public async Task<Group> GetGroup(Guid id)
    {
      using var context = contextFactory();
      var group = await context.Groups
        .FirstAsync(group => group.Id == id)
        .ConfigureAwait(false);
      return new(group.Id, group.Name, group.IsDefault);
    }

    public async Task<bool> GroupExists(string name, Guid? id = null)
    {
      using var context = contextFactory();
      var result = await context.Groups
        .FirstOrDefaultAsync(group => group.Name == name)
        .ConfigureAwait(false);
      return result != null && (!id.HasValue || result.Id != id.Value);
    }

    public async Task<bool> GroupExists(Guid id)
    {
      using var context = contextFactory();
      return await context.Groups
        .AnyAsync(group => group.Id == id)
        .ConfigureAwait(false);
    }

    public Groups ListGroups(
      string? filter,
      int skip = Application.IdentityConstants.List.Skip,
      int take = Application.IdentityConstants.List.Take
    )
    {
      using var context = contextFactory();
      var query = context.Groups
        .Where(group => string.IsNullOrEmpty(filter) ||
          EF.Functions.ILike(group.Name ?? "", $"%{filter}%")
        )
        .OrderBy(group => group.Name);

      var count = query.Count();
      var groups = query
        .Skip(skip)
        .Take(take == -1 ? count : take);

      return new Groups
      {
        TotalCount = count,
        Items = [.. groups.Select(g => new Group(g.Id, g.Name, g.IsDefault))]
      };
    }

    public async Task UpdateGroup(Group group)
    {
      using var context = contextFactory();

      var groupToBeUpdated = await context.Groups
        .FirstAsync(g => g.Id == group.Id)
        .ConfigureAwait(false);
      groupToBeUpdated.Name = group.Name;
      groupToBeUpdated.IsDefault = group.IsDefault;

      await context.SaveChangesAsync().ConfigureAwait(false);

      await publishEndpoint.Publish(new GroupUpdated(group.Id, group.Name, group.IsDefault)).ConfigureAwait(false);
    }
  }
}
