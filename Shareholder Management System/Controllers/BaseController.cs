using System.Web.Mvc;
using System.Linq;
using System;
using System.Collections.Generic;
using Shareholder_Management_System.Models;

public class BaseController : Controller
{
    private readonly Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

    protected override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        // Get controller and action from the current request
        string controller = filterContext.RouteData.Values["controller"]?.ToString().ToLower();
        string action = filterContext.RouteData.Values["action"]?.ToString().ToLower();
        // Skip permission check for SearchShareholders action
        if (action == "SearchShareholders")
        {
            base.OnActionExecuting(filterContext);
            return;
        }


        // Get user roles from session
        string userRoles = Session["Roles"]?.ToString();

        if (string.IsNullOrEmpty(userRoles))
        {
            filterContext.Result = RedirectToAction("Login", "UsersAuthorization");
            return;
        }

        // Split roles if multiple roles are stored as comma-separated
        var roles = userRoles.Split(',').Select(r => r.Trim().ToLower()).ToList();

        // Bypass permission check for SuperAdmin
        if (roles.Contains("superadmin"))
        {
            // SuperAdmin has full access, no permission check needed
            base.OnActionExecuting(filterContext);
            return;
        }

        // Check for SuperUser and restrict specific controllers
        if (roles.Contains("superuser"))
        {
            // List of restricted controllers for SuperUser
            var restrictedControllers = new List<string> { "users", "permissions", "branches1" };
            if (restrictedControllers.Contains(controller))
            {
                filterContext.Result = RedirectToAction("Unauthorized", "Home");
                return;
            }
            // SuperUser has access to all other controllers, no permission check needed
            base.OnActionExecuting(filterContext);
            return;
        }

        // Check permission for other roles using Entity Framework
        bool hasPermission = CheckPermission(roles, controller, action);

        if (!hasPermission)
        {
            filterContext.Result = RedirectToAction("Unauthorized", "Home");
            return;
        }

        base.OnActionExecuting(filterContext);
    }

    private bool CheckPermission(List<string> userRoles, string controller, string action)
    {
        try
        {
            // Check if any of the user's roles have permission for the controller and action
            bool hasPermission = db.Permissions
                .Any(p => userRoles.Contains(p.role_name.ToLower()) &&
                          p.perm_controller.ToLower() == controller &&
                          p.perm_action.ToLower() == action);

            return hasPermission;
        }
        catch (Exception)
        {
            // Log exception (implement proper logging)
            return false;
        }
    }

    // Dispose of the DbContext properly
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            db.Dispose();
        }
        base.Dispose(disposing);
    }
}