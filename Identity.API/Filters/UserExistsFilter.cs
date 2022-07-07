using Identity.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Identity.Filters
{
  public class UserExistsFilter : ActionFilterAttribute
  {
    private readonly IIdentityRepository _identityRepository;
    public UserExistsFilter(IIdentityRepository identityRepository)
    {
      _identityRepository = identityRepository;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
      var id = (long?)context.ActionArguments["id"];
      if (id == null || !_identityRepository.UserExists(id.Value).GetAwaiter().GetResult())
      {
        context.Result = new NotFoundResult();
      }
      base.OnActionExecuting(context);
    }
  }
}
