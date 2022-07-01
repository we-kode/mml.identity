
using AutoMapper;
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
    private readonly IMapper _mapper;

    public SqlGroupRepository(
      Func<ApplicationDBContext> contextFactory,
      IPublishEndpoint publishEndpoint,
      IMapper mapper
    )
    {
      _contextFactory = contextFactory;
      _publishEndpoint = publishEndpoint;
      _mapper = mapper;
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

      await context.Groups.AddAsync(group).ConfigureAwait(false);
      await context.SaveChangesAsync().ConfigureAwait(false);

      await _publishEndpoint.Publish<GroupCreated>(new
      {
        Id = groupId,
        Name = name,
        IsDefault = isDefault
      })
      .ConfigureAwait(false);

      return _mapper.Map<Group>(group);
    }

    public async Task DeleteGroup(Guid groupId)
    {
      using var context = _contextFactory();

      var group = await context.Groups
        .FirstOrDefaultAsync(group => group.Id == groupId)
        .ConfigureAwait(false);

      if (group == null)
      {
        return;
      }

      context.Groups.Remove(group);
      await context.SaveChangesAsync().ConfigureAwait(false);

      await _publishEndpoint.Publish<GroupDeleted>(new {
        Id = groupId
      }).ConfigureAwait(false);
    }

    public async Task<Group> GetGroup(Guid id)
    {
      using var context = _contextFactory();
      var group = await context.Groups
        .FirstAsync(group => group.Id == id)
        .ConfigureAwait(false);
      return _mapper.Map<Group>(group);
    }

    public async Task<bool> GroupExists(string name, Guid? id = null)
    {
      using var context = _contextFactory();
      var result = await context.Groups
        .FirstOrDefaultAsync(group => group.Name == name)
        .ConfigureAwait(false);
      return result != null && (!id.HasValue || result.Id != id.Value);
    }

    public async Task<bool> GroupExists(Guid id)
    {
      using var context = _contextFactory();
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
      using var context = _contextFactory();
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
        Items = _mapper.ProjectTo<Group>(groups).ToList()
      };
    }

    public async Task UpdateGroup(Group group)
    {
      using var context = _contextFactory();

      var groupToBeUpdated = await context.Groups
        .FirstAsync(g => g.Id == group.Id)
        .ConfigureAwait(false);
      groupToBeUpdated.Name = group.Name;
      groupToBeUpdated.IsDefault = group.IsDefault;

      await context.SaveChangesAsync().ConfigureAwait(false);

      await _publishEndpoint.Publish<GroupUpdated>(new
      {
        Id = group.Id,
        Name = group.Name,
        IsDefault = group.IsDefault
      })
      .ConfigureAwait(false);
    }
  }
}
