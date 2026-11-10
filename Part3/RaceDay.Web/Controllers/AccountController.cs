using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // Register, log in and log out. Each one is a call to the Part 2 API; on success the user's
    // id, role and name are kept in this website's session for the role-based menus.
    public class AccountController : BaseController
    {
        public AccountController(ApiClient api) : base(api)
        {
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (HttpContext.Session.IsLoggedIn())
            {
                return RedirectToHome();
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await Api.LoginAsync(new LoginRequest { Email = model.Email, Password = model.Password });
            if (!result.Success || result.Data is null)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View(model);
            }

            StartSession(result.Data);
            Success($"Welcome back, {result.Data.FullName}.");

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToHome();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.IsLoggedIn())
            {
                return RedirectToHome();
            }

            return View(new RegisterViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var register = await Api.RegisterAsync(new RegisterRequest
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = model.Password,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                City = model.City,
                RoleId = model.RoleId
            });

            if (!register.Success)
            {
                ModelState.AddModelError(string.Empty, register.Error);
                return View(model);
            }

            // Log the new user straight in so they land on their own home page.
            var login = await Api.LoginAsync(new LoginRequest { Email = model.Email, Password = model.Password });
            if (!login.Success || login.Data is null)
            {
                Success("Your account was created. Please log in.");
                return RedirectToAction(nameof(Login));
            }

            StartSession(login.Data);
            Success($"Welcome to RaceDay, {login.Data.FullName}!");
            return RedirectToHome();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // End the API session first, then clear this website's session.
            await Api.LogoutAsync();
            HttpContext.Session.Clear();

            Success("You have been logged out.");
            return RedirectToAction(nameof(Login));
        }

        private void StartSession(AuthResponse user)
        {
            HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
            HttpContext.Session.SetString(SessionKeys.Role, user.RoleName);
            HttpContext.Session.SetString(SessionKeys.FullName, user.FullName);
        }

        private IActionResult RedirectToHome() =>
            HttpContext.Session.IsOrganiser()
                ? RedirectToAction("Dashboard", "Organiser")
                : RedirectToAction("Index", "Home");
    }
}
