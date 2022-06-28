using System;

namespace Identity.DBContext.Models
{
  public class Group
  {
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsDefault { get; set; }
  }
}
