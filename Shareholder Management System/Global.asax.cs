using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Shareholder_Management_System.commons;
namespace Shareholder_Management_System
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
        protected void Application_BeginRequest()
        {
            HttpContext.Current.Items["RequestStartTime"] = DateTime.UtcNow;
            HttpContext.Current.Response.Headers.Add("Content-Security-Policy",
             "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';");
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
            Response.Cache.SetNoStore();
        }

        protected void Application_EndRequest()
        {
            var context = HttpContext.Current;

            var start = (DateTime)context.Items["RequestStartTime"];
            var duration = DateTime.UtcNow - start;

            var endpoint = context.Request.Url.AbsolutePath;
            var method = context.Request.HttpMethod;
            var status = context.Response.StatusCode.ToString();

            AppMetrics.HttpRequests.WithLabels(method, endpoint, status).Inc();
            AppMetrics.HttpRequestDuration.WithLabels(endpoint).Observe(duration.TotalSeconds);
        }
    }
}
