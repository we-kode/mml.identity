
using System;

namespace Identity.DBContext.Models
{
  public class ClientGroup
  {
    public string ClientId { get; set; } = null!;
    public OpenIddictClientApplication Client { get; set; } = null!;

    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;
  }
}
