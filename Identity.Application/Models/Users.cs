using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.Models
{
  /// <summary>
  /// List of <see cref="User"/>
  /// </summary>
  public class Users
  {
    /// <summary>
    /// The total count of available users
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// On chunk of loaded users.
    /// </summary>
    public IList<User> Items { get; set; }

    /// <summary>
    /// Constructs new Instance
    /// </summary>
    public Users()
    {
      Items = new List<User>();
    }
  }
}
