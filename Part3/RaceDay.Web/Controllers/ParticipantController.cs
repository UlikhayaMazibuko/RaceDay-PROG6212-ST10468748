using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // This controller has the pages only participants can use: entering an event, My Events and My Results
    [RequireLogin(Roles.Participant)]
    public class ParticipantController : BaseController
    {
        public ParticipantController(ApiClient api) : base(api)
        {
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Enter(int categoryId, int eventId)
        {
            var result = await Api.EnrolAsync(categoryId);
            if (SessionExpired(result)) return ToLogin();

            if (!result.Success || result.Data is null)
            {
                Error(result.Error);
                return RedirectToAction("Details", "Home", new { id = eventId });
            }

            Success($"You're entered! Your race number is {result.Data.RaceNumber}. Status: {result.Data.EnrolmentStatus}.");
            return RedirectToAction(nameof(MyEvents));
        }

        public async Task<IActionResult> MyEvents()
        {
            var result = await Api.GetMyEnrolmentsAsync();
            if (SessionExpired(result)) return ToLogin();
            if (!result.Success) Error(result.Error);

            return View(result.Data ?? new());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int enrolmentId)
        {
            var result = await Api.WithdrawAsync(enrolmentId);
            if (SessionExpired(result)) return ToLogin();

            if (result.Success) Success("You have withdrawn from that event.");
            else Error(result.Error);

            return RedirectToAction(nameof(MyEvents));
        }

        public async Task<IActionResult> MyResults()
        {
            var result = await Api.GetMyResultsAsync();
            if (SessionExpired(result)) return ToLogin();
            if (!result.Success) Error(result.Error);

            return View(result.Data ?? new());
        }
    }
}
