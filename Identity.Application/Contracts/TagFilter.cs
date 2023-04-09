using System;
using System.Collections.Generic;

namespace Identity.Application.Contracts;

/// <summary>
/// Includes ids of tags for which the list should be filtered.
/// </summary>
public class TagFilter
{
  /// <summary>
  /// List of groups ids.
  /// </summary>
  public IList<Guid> Groups { get; set; }

  public TagFilter()
  {
    Groups = new List<Guid>();
  }
}
