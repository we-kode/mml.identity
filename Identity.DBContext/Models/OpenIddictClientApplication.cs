using OpenIddict.EntityFrameworkCore.Models;

namespace Identity.DBContext.Models
{
  public class OpenIddictClientApplication : OpenIddictEntityFrameworkCoreApplication<string, OpenIddictClientAuthorization, OpenIddictClientToken>
  {
    public string? PublicKey { get; set; }
  }
}
