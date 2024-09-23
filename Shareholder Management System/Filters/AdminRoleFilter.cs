using System;
using System.Web.Mvc;

public class AdminRoleFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        // Get the action name
        string actionName = filterContext.ActionDescriptor.ActionName;

        // If the action is "Profile", skip the admin role check
        if (actionName.Equals("Profile", StringComparison.OrdinalIgnoreCase))
        {
            base.OnActionExecuting(filterContext);
            return;
        }

        // If the action is "Profile", skip the admin role check
        if (actionName.Equals("ResetPasswordOwn", StringComparison.OrdinalIgnoreCase))
        {
            base.OnActionExecuting(filterContext);
            return;
        }


        // Check if the user is logged in and has the 'Administrator' role
        var roles = filterContext.HttpContext.Session["Roles"];
        if (roles == null || !roles.ToString().Equals("Administrator"))
        {
            // Redirect to Unauthorized page if not an Administrator
            filterContext.Result = new RedirectToRouteResult(
                new System.Web.Routing.RouteValueDictionary(new { controller = "Home", action = "Unauthorized" })
            );
        }

        base.OnActionExecuting(filterContext);
    }
}
