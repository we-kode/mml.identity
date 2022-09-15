using OpenIddict.EntityFrameworkCore.Models;

namespace Identity.DBContext.Models
{
  public class OpenIddictClientToken : OpenIddictEntityFrameworkCoreToken<string, OpenIddictClientApplication, OpenIddictClientAuthorization> { }
}
