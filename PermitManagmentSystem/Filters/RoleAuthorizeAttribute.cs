using System.Web.Mvc;

namespace PermitManagmentSystem.Filters
{
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                // signed in but the wrong role: show 403, do NOT redirect to Login
                filterContext.Result = new HttpStatusCodeResult(403, "You do not have access to this page.");
            }
            else
            {
                base.HandleUnauthorizedRequest(filterContext);   // not signed in -> Login
            }
        }
    }
}