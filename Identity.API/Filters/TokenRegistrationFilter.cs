using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace Identity.Filters
{
  // Validates the registration token for client registration
  public class TokenRegistrationFilter(IDistributedCache cache) : ActionFilterAttribute
  {
    public override void OnActionExecuting(ActionExecutingContext context)
    {
      var token = (string?)context.ActionArguments["regToken"];
      if (string.IsNullOrEmpty(token))
      {
        context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
      }

      // get the connection id of the exiting token. Needed to inform admin for a valid client registration
      var connectionId = cache.GetString(token!);
      if (string.IsNullOrEmpty(connectionId))
      {
        context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
      }

      // Add the connection id to the query
      context.ActionArguments.Add("conId", connectionId);
      cache.Remove(token!);

      base.OnActionExecuting(context);
    }
  }
}
