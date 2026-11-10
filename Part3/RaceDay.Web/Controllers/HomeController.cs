using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // This controller has the public pages: the events list with filters and the event details page
    public class HomeController : BaseController
    {
        public HomeController(ApiClient api) : base(api)
        {
        }

        public async Task<IActionResult> Index(string? city, int? disciplineId, DateOnly? fromDate)
        {
            // If no date is picked we only show events from today onwards
            var from = fromDate ?? DateOnly.FromDateTime(DateTime.Today);

            var model = new HomeViewModel { City = city, DisciplineId = disciplineId, FromDate = from };

            var events = await Api.GetEventsAsync(city, disciplineId, from);
            if (events.Success)
            {
                model.Events = events.Data ?? new();
            }
            else
            {
                Error(events.Error);
            }

            var disciplines = await Api.GetDisciplinesAsync();
            model.Disciplines = disciplines.Data ?? new();

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var ev = await Api.GetEventAsync(id);
            if (!ev.Success || ev.Data is null)
            {
                Error(ev.Error);
                return RedirectToAction(nameof(Index));
            }

            var categories = await Api.GetCategoriesAsync(id);
            var results = await Api.GetEventResultsAsync(id);

            var model = new EventDetailsViewModel
            {
                Event = ev.Data,
                Categories = categories.Data ?? new(),
                Results = results.Data ?? new(),
                EntriesOpen = ev.Data.Status == "Published"
                    && ev.Data.RegistrationCloseDate >= DateOnly.FromDateTime(DateTime.Today)
            };

            // Gets the participant's enrolments so the page shows "Entered" instead of the Enter button
            if (HttpContext.Session.IsParticipant())
            {
                var mine = await Api.GetMyEnrolmentsAsync();
                model.MyCategoryIds = (mine.Data ?? new())
                    .Where(en => en.EventId == id)
                    .Select(en => en.CategoryId)
                    .ToHashSet();
            }

            return View(model);
        }

        public IActionResult Error(int? code = null)
        {
            var model = new ErrorViewModel { StatusCode = code };

            if (code == 404)
            {
                model.Message = "We could not find that page.";
            }
            else if (HttpContext.Features.Get<IExceptionHandlerPathFeature>() is not null)
            {
                model.Message = "Something went wrong while loading that page. Please try again.";
            }

            return View(model);
        }
    }
}
