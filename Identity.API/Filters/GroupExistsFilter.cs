using Identity.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace Identity.Filters
{
  public class GroupExistsFilter : ActionFilterAttribute
  {
    private readonly IGroupRepository _groupRepository;
    public GroupExistsFilter(IGroupRepository groupRepository)
    {
      _groupRepository = groupRepository;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
      var id = (Guid?)context.ActionArguments["id"];
      if (id == null || !_groupRepository.GroupExists(id.Value).GetAwaiter().GetResult())
      {
        context.Result = new NotFoundResult();
      }
      base.OnActionExecuting(context);
    }
  }
}
