using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // This controller lets the user view and update their profile and upload a profile picture.
    // The picture is sent to the API, and the API saves it in Azure
    [RequireLogin]
    public class ProfileController : BaseController
    {
        public ProfileController(ApiClient api) : base(api)
        {
        }

        public async Task<IActionResult> Index()
        {
            var result = await Api.GetProfileAsync();
            if (SessionExpired(result)) return ToLogin();

            if (!result.Success || result.Data is null)
            {
                Error(result.Error);
                return RedirectToAction("Index", "Home");
            }

            return View(ToViewModel(result.Data));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            // Date of birth is only shown on this page, it can't be changed, so we don't validate it
            ModelState.Remove(nameof(model.DateOfBirth));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await Api.UpdateProfileAsync(new UpdateProfileRequest
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Gender = model.Gender,
                City = model.City
            });
            if (SessionExpired(result)) return ToLogin();

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View(model);
            }

            HttpContext.Session.SetString(SessionKeys.FullName, $"{model.FirstName} {model.LastName}");
            Success("Your profile has been updated.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(IFormFile? picture)
        {
            if (picture is null || picture.Length == 0)
            {
                Error("Please choose an image to upload.");
                return RedirectToAction(nameof(Index));
            }

            var problem = CheckImage(picture);
            if (problem is not null)
            {
                Error(problem);
                return RedirectToAction(nameof(Index));
            }

            var result = await Api.UploadProfilePictureAsync(picture);
            if (SessionExpired(result)) return ToLogin();

            if (result.Success) Success("Your profile picture has been updated.");
            else Error(result.Error);

            return RedirectToAction(nameof(Index));
        }

        private static ProfileViewModel ToViewModel(UserProfile profile) => new()
        {
            Email = profile.Email,
            RoleName = profile.RoleName,
            DateOfBirth = profile.DateOfBirth,
            ProfilePictureUrl = profile.ProfilePictureUrl,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            PhoneNumber = profile.PhoneNumber,
            Gender = profile.Gender,
            City = profile.City
        };
    }
}
