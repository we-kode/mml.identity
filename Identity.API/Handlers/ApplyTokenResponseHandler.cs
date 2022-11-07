using OpenIddict.Server;
using System.Threading.Tasks;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Identity.Handlers
{
  public class ApplyTokenResponseHandler : IOpenIddictServerHandler<ApplyTokenResponseContext>
  {
    public ValueTask HandleAsync(ApplyTokenResponseContext context)
    {
      var response = context.Response;
      if (!string.IsNullOrEmpty(response.Error) && !string.IsNullOrEmpty(response.ErrorDescription))
      {
        response.Error = "Error occurred 333";
        response.ErrorDescription = string.Empty;
        response.ErrorUri = string.Empty;
      }

      return default;
    }
  }
}
