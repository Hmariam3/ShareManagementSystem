using System.Web.Mvc;
using System.Text;

public static class BreadcrumbHelper
{
    public static MvcHtmlString GenerateBreadcrumb(ControllerBase controller)
    {
        string controllerName = controller.ControllerContext.RouteData.Values["controller"].ToString();
        string actionName = controller.ControllerContext.RouteData.Values["action"].ToString();

        StringBuilder breadcrumb = new StringBuilder();
        breadcrumb.Append("<nav aria-label='breadcrumb'><div class='flex items-center space-x-2 text-gray-700'>");
        breadcrumb.Append("<span class='text-blue-600 hover:underline'><a href='/Home/Index'>Home</a></span>"); // Link for Home

        // Add custom logic for specific actions
        if (controllerName == "Users")
        {
            if (actionName == "Index")
            {
                breadcrumb.Append(" <span>/</span><span class='font-semibold'>View Users</span>");
            }
            else if (actionName == "Create")
            {
                breadcrumb.Append(" <span>/</span><span class='font-semibold'>Create User</span>");
            }
            else if (actionName == "Details")
            {
                breadcrumb.Append(" <span>/</span><span class='font-semibold'>User Details</span>");
            }
            else if (actionName == "Edit")
            {
                breadcrumb.Append(" <span>/</span><span class='font-semibold'>User Edit</span>");
            }
            else if (actionName == "Profile")
            {
                breadcrumb.Append(" <span>/</span><span class='font-semibold'>User Profile</span>");
            }
            else if (actionName == "View")
            {
                breadcrumb.Append(" <span>/</span><span><a href='/Users' class='text-blue-600 hover:underline'>Profile</a></span>");
                breadcrumb.Append(" <span>/</span><span class='font-semibold'>View</span>");
            }
            // Add more routes as necessary
        }

        breadcrumb.Append("</div></nav>");
        return MvcHtmlString.Create(breadcrumb.ToString());
    }
}
