using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters
{
    // This filter protects the pages that need a login.
    // If nobody is logged in it goes to the login page, and if the role is wrong it goes back to their own home page
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireLoginAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public RequireLoginAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var http = context.HttpContext;
            var session = http.Session;

            if (!session.IsLoggedIn())
            {
                var returnUrl = http.Request.Path + http.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            if (_roles.Length > 0 && !_roles.Contains(session.GetRole()))
            {
                var tempData = http.RequestServices.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(http);
                tempData["Error"] = $"That page is only for {string.Join(" and ", _roles)}s.";

                context.Result = session.IsOrganiser()
                    ? new RedirectToActionResult("Dashboard", "Organiser", null)
                    : new RedirectToActionResult("Index", "Home", null);
            }
        }
    }
}
