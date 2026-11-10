using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // Shared helpers for every page controller.
    public abstract class BaseController : Controller
    {
        protected readonly ApiClient Api;

        protected BaseController(ApiClient api)
        {
            Api = api;
        }

        protected void Success(string message) => TempData["Success"] = message;

        protected void Error(string message) => TempData["Error"] = message;

        // If the API says the user is no longer logged in (for example the API was restarted),
        // clear this website's session too and send them to log in again.
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

        // Checks an uploaded image before it is sent to the API.
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
