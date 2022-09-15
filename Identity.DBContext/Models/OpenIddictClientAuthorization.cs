using OpenIddict.EntityFrameworkCore.Models;

namespace Identity.DBContext.Models
{
  public class OpenIddictClientAuthorization : OpenIddictEntityFrameworkCoreAuthorization<string, OpenIddictClientApplication, OpenIddictClientToken> { }
}
