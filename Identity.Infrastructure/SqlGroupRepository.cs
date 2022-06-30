
using Identity.Application.Contracts;
using Identity.Application.Models;
using Identity.DBContext;
using MassTransit;
using Messages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using DbGroup = Identity.DBContext.Models.Group;

namespace Identity.Infrastructure
{
  public class SqlGroupRepository : IGroupRepository
  {
    private readonly Func<ApplicationDBContext> _contextFactory;
    private readonly IPublishEndpoint _publishEndpoint;

    public SqlGroupRepository(
      Func<ApplicationDBContext> contextFactory,
      IPublishEndpoint publishEndpoint
    )
    {
      _contextFactory = contextFactory;
      _publishEndpoint = publishEndpoint;
    }

    public async Task<Group> CreateNewGroup(string name, bool isDefault)
    {
      using var context = _contextFactory();
      var groupId = Guid.NewGuid();
      var group = new DbGroup
      {
        Id = groupId,
        Name = name,
        IsDefault = isDefault
      };

      await context.Groups.AddAsync(group);
      await context.SaveChangesAsync();

      await _publishEndpoint.Publish<GroupCreated>(new
      {
        Id = groupId,
        Name = name,
        IsDefault = isDefault
      });

      return MapModel(group);
    }

    public async Task DeleteGroup(Guid groupId)
    {
      using var context = _contextFactory();
      var group = await context.Groups.FirstOrDefaultAsync(group => group.Id == groupId);
      if (group == null)
      {
        return;
      }

      context.Groups.Remove(group);
      await context.SaveChangesAsync();

      await _publishEndpoint.Publish<GroupDeleted>(new {
        Id = groupId
      });
    }

    public async Task<Group> GetGroup(Guid id)
    {
      using var context = _contextFactory();
      var group = await context.Groups.FirstAsync(group => group.Id == id);
      return MapModel(group);
    }

    public async Task<bool> GroupExists(string name, Guid? id = null)
    {
      using var context = _contextFactory();
      var result = await context.Groups.FirstOrDefaultAsync(group => group.Name == name);
      return result != null && (!id.HasValue || result.Id != id.Value);
    }

    public async Task<bool> GroupExists(Guid id)
    {
      using var context = _contextFactory();
      return await context.Groups.AnyAsync(group => group.Id == id);
    }

    public Groups ListGroups(string? filter, int skip = Application.IdentityConstants.List.Skip, int take = Application.IdentityConstants.List.Take)
    {
      using var context = _contextFactory();
      var query = context.Groups
        .Where(group => string.IsNullOrEmpty(filter) || EF.Functions.ILike(group.Name ?? "", $"%{filter}%"))
        .OrderBy(group => group.Name);

      var count = query.Count();
      var groups = query
        .Select(group => MapModel(group))
        .Skip(skip)
        .Take(take == -1 ? count : take)
        .ToList();

      return new Groups
      {
        TotalCount = count,
        Items = groups
      };
    }

    public async Task UpdateGroup(Group group)
    {
      using var context = _contextFactory();

      var groupToBeUpdated = await context.Groups.FirstAsync(g => g.Id == group.Id);
      groupToBeUpdated.Name = group.Name;
      groupToBeUpdated.IsDefault = group.IsDefault;

      await context.SaveChangesAsync();

      await _publishEndpoint.Publish<GroupUpdated>(new
      {
        Id = group.Id,
        Name = group.Name,
        IsDefault = group.IsDefault
      });
    }

    private static Group MapModel(DbGroup group)
    {
      return new Group
      (
        group.Id,
        group.Name,
        group.IsDefault
      );
    }
  }
}
