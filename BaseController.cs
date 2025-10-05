using System;
using System.Web;
using System.Web.Mvc;

namespace MU_AttendanceSystem.Controllers
{
    /// <summary>
    /// All your controllers should inherit from this so that unauthenticated
    /// users get redirected to Login and you disable browser caching.
    /// </summary>
    public abstract class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Allow Account/Login, Logout or AccessDenied to run without redirect loop
            var ctrl = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
            var action = filterContext.ActionDescriptor.ActionName;
            if (ctrl.Equals("Account", StringComparison.OrdinalIgnoreCase) &&
                (action.Equals("Login", StringComparison.OrdinalIgnoreCase) ||
                 action.Equals("Logout", StringComparison.OrdinalIgnoreCase) ||
                 action.Equals("AccessDenied", StringComparison.OrdinalIgnoreCase)))
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            // 1) If there's no UserID in session, redirect to /Account/Login
            if (Session["UserID"] == null)
            {
                filterContext.Result = RedirectToAction("Login", "Account");
                return;
            }

            // 2) Disable browser caching for every other page
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            base.OnActionExecuting(filterContext);
        }
    }
}
