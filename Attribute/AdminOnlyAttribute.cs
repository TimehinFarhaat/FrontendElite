using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class AdminOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var isAdmin = context.HttpContext.Session.GetString("IsAdmin");
        if (string.IsNullOrWhiteSpace(isAdmin) || isAdmin != "true")
        {
            // Redirect to admin login if not logged in
            context.Result = new RedirectToActionResult("Login", "Admin", null);
        }
        base.OnActionExecuting(context);
    }
}
