using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // This is the base class for the other controllers so they can share the same helper methods
    public abstract class BaseController : Controller
    {
        protected readonly ApiClient Api;

        protected BaseController(ApiClient api)
        {
            Api = api;
        }

        protected void Success(string message) => TempData["Success"] = message;

        protected void Error(string message) => TempData["Error"] = message;

        // If the API returns 401 it means the API session ended (for example the API was restarted),
        // so we clear our session as well and send the user back to the login page
        protected bool SessionExpired<T>(ApiResult<T> result)
        {
            if (result.StatusCode != StatusCodes.Status401Unauthorized)
            {
                return false;
            }

            HttpContext.Session.Clear();
            Error("Your session has expired. Please log in again.");
            return true;
        }

        protected IActionResult ToLogin() => RedirectToAction("Login", "Account");

        // Checks the image type and size before sending it to the API
        protected static string? CheckImage(IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                return null;
            }

            var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowed.Contains(file.ContentType))
            {
                return "Please upload a JPG, PNG, WEBP or GIF image.";
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return "Images must be 5 MB or smaller.";
            }

            return null;
        }
    }
}
