using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace EProject.NETCore.Filter;

public class SessionCheckAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var username = context.HttpContext.Session.GetString("UserID");
        if (string.IsNullOrEmpty(username))
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "User", new { returnUrl });
        }
        base.OnActionExecuting(context);
    }
}
