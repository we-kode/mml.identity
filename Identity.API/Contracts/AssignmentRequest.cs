using System;
using System.Collections.Generic;

namespace Identity.Contracts
{
  /// <summary>
  /// Request for assignment to groups
  /// </summary>
  public class AssignmentRequest
  {
    /// <summary>
    /// Items to be assigned.
    /// </summary>
    public List<string> Items { get; set; } = new List<string>();

    /// <summary>
    /// Listz of groups to which the items should be assigned.
    /// </summary>
    public List<Guid> Groups { get; set; } = new List<Guid>();

    /// <summary>
    /// Initial assigned groups.
    /// </summary>
    public List<Guid> InitGroups { get; set; } = new List<Guid>();
  }
}
