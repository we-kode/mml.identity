using OpenIddict.EntityFrameworkCore.Models;
using System;
using System.Collections.Generic;

namespace Identity.DBContext.Models
{
  public class OpenIddictClientApplication : OpenIddictEntityFrameworkCoreApplication<string, OpenIddictClientAuthorization, OpenIddictClientToken>
  {
    public string? PublicKey { get; set; }
    public string DeviceIdentifier { get; set; } = string.Empty;
    public DateTime LastTokenRefreshDate { get; set; }
    public ICollection<Group> Groups { get; set; } = new List<Group>();
  }
}
