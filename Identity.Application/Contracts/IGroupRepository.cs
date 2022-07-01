
using Identity.Application.IdentityConstants;
using Identity.Application.Models;
using System;
using System.Threading.Tasks;

namespace Identity.Application.Contracts
{
  public interface IGroupRepository
  {
    /// <summary>
    /// Returns a list of all exisiting groups.
    /// </summary>
    /// <param name="filter">Groups will be filtered by given string.</param>
    /// <param name="skip">Offset of the list.</param>
    /// <param name="take">Size of chunk to be loaded.</param>
    /// <returns><see cref="Groups"/></returns>
    Groups ListGroups(string? filter, int skip = List.Skip, int take = List.Take);

    /// <summary>
    /// Returns a boolean, that indicates whether a group with the given name exists.
    /// Excepting the group with the passed id.
    /// </summary>
    /// <param name="name">Name to check for.</param>
    /// <param name="id">Id to ignore if passed.</param>
    /// <returns>Boolean, that indicates whether a group with the given name exists.</returns>
    Task<bool> GroupExists(string name, Guid? id = null);

    /// <summary>
    /// Returns a boolean, that indicates whether a group with the given id exists.
    /// </summary>
    /// <param name="id">Id to check for.</param>
    /// <returns>Boolean, that indicates whether a group with the given id exists.</returns>
    Task<bool> GroupExists(Guid id);

    /// <summary>
    /// Creates a new group with the passed name.
    /// </summary>
    /// <param name="name">Name of the new group.</param>
    /// <param name="isDefault">Flag whether the group is a default group.</param>
    /// <returns>The newly created group.</returns>
    Task<Group> CreateNewGroup(string name, bool isDefault);

    /// <summary>
    /// Returns the group with the given id.
    /// </summary>
    /// <param name="id">Id of the group the be loaded.</param>
    /// <returns>Returns the group with the given id.</returns>
    Task<Group> GetGroup(Guid id);

    /// <summary>
    /// Updates the passed group.
    /// </summary>
    /// <param name="group">Group with new data to be updated.</param>
    Task UpdateGroup(Group group);

    /// <summary>
    /// Deletes a group with the given id.
    /// </summary>
    /// <param name="groupId">Id of group to be deleted.</param>
    Task DeleteGroup(Guid groupId);
  }
}
