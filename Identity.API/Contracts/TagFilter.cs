using System.Collections.Generic;
using System;

namespace Identity.Contracts;

  /// <summary>
  /// Includes ids of tags for which the list should be filtered.
  /// </summary>
  public class TagFilter
  {
    /// <summary>
    /// List of group ids.
    /// </summary>
    public IList<Guid> Groups { get; set; }

    /// <summary>
    /// Show only new once.
    /// </summary>
    public bool OnlyNew {  get; set; }

    public TagFilter()
    {
      Groups = new List<Guid>();
    }
  }
