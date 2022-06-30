using System;
using System.Collections.Generic;

namespace Identity.DBContext.Models
{
  public class Group
  {
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsDefault { get; set; }
    public ICollection<OpenIddictClientApplication> Clients { get; set; } = new List<OpenIddictClientApplication>();
  }
}
