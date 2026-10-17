using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Security
{
  
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public RequireRoleAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.GetCurrentUserId();
            var role = context.HttpContext.GetCurrentRole();

            if (userId is null || string.IsNullOrEmpty(role))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "You must be logged in to use this endpoint." });
                return;
            }

            if (_allowedRoles.Length > 0 && !_allowedRoles.Contains(role))
            {
                context.Result = new ObjectResult(new { message = $"This endpoint is only available to: {string.Join(", ", _allowedRoles)}." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}