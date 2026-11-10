using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // Organiser-only pages: dashboard, event management (with banner upload), categories,
    // enrolments and results entry. The API also checks that the organiser owns each event.
    [RequireLogin(Roles.Organiser)]
    public class OrganiserController : BaseController
    {
        public OrganiserController(ApiClient api) : base(api)
        {
        }

        // ---------- Dashboard ----------
        public async Task<IActionResult> Dashboard()
        {
            var result = await Api.GetMyEventsAsync();
            if (SessionExpired(result)) return ToLogin();
            if (!result.Success) Error(result.Error);

            var events = result.Data ?? new();
            var today = DateOnly.FromDateTime(DateTime.Today);

            return View(new DashboardViewModel
            {
                Events = events,
                Upcoming = events.Where(e => e.EventDate >= today && e.Status != "Cancelled")
                                 .OrderBy(e => e.EventDate)
                                 .ToList()
            });
        }

        // ---------- Create event ----------
        [HttpGet]
        public async Task<IActionResult> CreateEvent()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var model = new EventFormViewModel
            {
                EventDate = today.AddMonths(1),
                RegistrationCloseDate = today.AddMonths(1).AddDays(-7),
                StartTime = new TimeOnly(6, 0)
            };
            await LoadDisciplines(model);
            return View("EventForm", model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventFormViewModel model)
        {
            if (model.DistanceKm is null)
            {
                ModelState.AddModelError(nameof(model.DistanceKm), "Please enter the distance for the first category.");
            }

            var imageProblem = CheckImage(model.Banner);
            if (imageProblem is not null)
            {
                ModelState.AddModelError(nameof(model.Banner), imageProblem);
            }

            if (!ModelState.IsValid)
            {
                await LoadDisciplines(model);
                return View("EventForm", model);
            }

            var created = await Api.CreateEventAsync(ToRequest(model, includeStatus: false));
            if (SessionExpired(created)) return ToLogin();

            if (!created.Success || created.Data is null)
            {
                ModelState.AddModelError(string.Empty, created.Error);
                await LoadDisciplines(model);
                return View("EventForm", model);
            }

            var eventId = created.Data.EventId;
            var messages = new List<string> { $"\"{created.Data.EventName}\" has been created." };

            // The event's first distance becomes its first category.
            var categoryName = string.IsNullOrWhiteSpace(model.FirstCategoryName)
                ? $"{model.DistanceKm:0.##} km"
                : model.FirstCategoryName.Trim();

            var category = await Api.CreateCategoryAsync(eventId, new CategoryRequest
            {
                CategoryName = categoryName,
                DistanceKm = model.DistanceKm,
                EntryFee = model.EntryFee
            });
            if (!category.Success) messages.Add($"The first category could not be added: {category.Error}");

            if (model.Banner is not null && model.Banner.Length > 0)
            {
                var upload = await Api.UploadEventBannerAsync(eventId, model.Banner);
                if (!upload.Success) messages.Add($"The banner could not be uploaded: {upload.Error}");
            }

            if (messages.Count == 1) Success(messages[0]);
            else Error(string.Join(" ", messages));

            return RedirectToAction(nameof(Categories), new { id = eventId });
        }

        // ---------- Edit event ----------
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var ev = await Api.GetEventAsync(id);
            if (!ev.Success || ev.Data is null)
            {
                Error(ev.Error);
                return RedirectToAction(nameof(Dashboard));
            }

            if (ev.Data.OrganiserId != HttpContext.Session.GetInt32(SessionKeys.UserId))
            {
                Error("You can only edit events you created.");
                return RedirectToAction(nameof(Dashboard));
            }

            var model = new EventFormViewModel
            {
                EventId = ev.Data.EventId,
                DisciplineId = ev.Data.DisciplineId,
                EventName = ev.Data.EventName,
                Description = ev.Data.Description,
                EventDate = ev.Data.EventDate,
                StartTime = ev.Data.StartTime,
                VenueName = ev.Data.VenueName,
                City = ev.Data.City,
                Province = ev.Data.Province,
                RegistrationCloseDate = ev.Data.RegistrationCloseDate,
                Status = ev.Data.Status,
                CurrentBannerUrl = ev.Data.BannerImageUrl
            };
            await LoadDisciplines(model);
            return View("EventForm", model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
        {
            model.EventId = id;

            var imageProblem = CheckImage(model.Banner);
            if (imageProblem is not null)
            {
                ModelState.AddModelError(nameof(model.Banner), imageProblem);
            }

            if (!ModelState.IsValid)
            {
                await LoadDisciplines(model);
                return View("EventForm", model);
            }

            var updated = await Api.UpdateEventAsync(id, ToRequest(model, includeStatus: true));
            if (SessionExpired(updated)) return ToLogin();

            if (!updated.Success)
            {
                ModelState.AddModelError(string.Empty, updated.Error);
                await LoadDisciplines(model);
                return View("EventForm", model);
            }

            if (model.Banner is not null && model.Banner.Length > 0)
            {
                var upload = await Api.UploadEventBannerAsync(id, model.Banner);
                if (!upload.Success)
                {
                    Error($"The event was saved, but the banner could not be uploaded: {upload.Error}");
                    return RedirectToAction(nameof(Dashboard));
                }
            }

            Success("The event has been updated.");
            return RedirectToAction(nameof(Dashboard));
        }

        // ---------- Delete event ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var result = await Api.DeleteEventAsync(id);
            if (SessionExpired(result)) return ToLogin();

            if (result.Success) Success("The event has been deleted.");
            else Error(result.Error);

            return RedirectToAction(nameof(Dashboard));
        }

        // ---------- Categories ----------
        [HttpGet]
        public async Task<IActionResult> Categories(int id)
        {
            var model = await LoadCategoriesPage(id);
            if (model is null) return RedirectToAction(nameof(Dashboard));
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(int id, [Bind(Prefix = "NewCategory")] CategoryFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                var page = await LoadCategoriesPage(id);
                if (page is null) return RedirectToAction(nameof(Dashboard));
                page.NewCategory = form;
                return View(nameof(Categories), page);
            }

            var result = await Api.CreateCategoryAsync(id, new CategoryRequest
            {
                CategoryName = form.CategoryName,
                DistanceKm = form.DistanceKm,
                EntryFee = form.EntryFee,
                MaxParticipants = form.MaxParticipants,
                MinimumAge = form.MinimumAge,
                CategoryStartTime = form.CategoryStartTime
            });
            if (SessionExpired(result)) return ToLogin();

            if (result.Success) Success($"Category \"{form.CategoryName}\" added.");
            else Error(result.Error);

            return RedirectToAction(nameof(Categories), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id, int categoryId)
        {
            var result = await Api.DeleteCategoryAsync(categoryId);
            if (SessionExpired(result)) return ToLogin();

            if (result.Success) Success("The category has been removed.");
            else Error(result.Error);

            return RedirectToAction(nameof(Categories), new { id });
        }

        // ---------- Enrolments ----------
        public async Task<IActionResult> Enrolments(int id)
        {
            var ev = await LoadOwnEvent(id);
            if (ev is null) return RedirectToAction(nameof(Dashboard));

            var enrolments = await Api.GetEventEnrolmentsAsync(id);
            if (SessionExpired(enrolments)) return ToLogin();
            if (!enrolments.Success) Error(enrolments.Error);

            return View(new EnrolmentsViewModel { Event = ev, Enrolments = enrolments.Data ?? new() });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEnrolmentStatus(int id, int enrolmentId, string status)
        {
            var result = await Api.UpdateEnrolmentStatusAsync(enrolmentId, status);
            if (SessionExpired(result)) return ToLogin();

            if (result.Success) Success($"Enrolment status changed to {status}.");
            else Error(result.Error);

            return RedirectToAction(nameof(Enrolments), new { id });
        }

        // ---------- Results ----------
        public async Task<IActionResult> Results(int id)
        {
            var ev = await LoadOwnEvent(id);
            if (ev is null) return RedirectToAction(nameof(Dashboard));

            var enrolments = await Api.GetEventEnrolmentsAsync(id);
            if (SessionExpired(enrolments)) return ToLogin();
            var results = await Api.GetEventResultsAsync(id);

            return View(new ResultsEntryViewModel
            {
                Event = ev,
                Enrolments = (enrolments.Data ?? new()).Where(en => en.EnrolmentStatus != "Cancelled").ToList(),
                ResultsByEnrolment = (results.Data ?? new()).ToDictionary(r => r.EnrolmentId),
                EventHasHappened = ev.EventDate <= DateOnly.FromDateTime(DateTime.Today)
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CaptureResult(int id, int enrolmentId, string resultStatus,
            TimeOnly? finishTime, int? positionOverall, int? positionInCategory)
        {
            // DNF, DNS and DQ have no time or positions, so anything typed in is ignored for them.
            var finished = resultStatus == "Finished";

            var result = await Api.CaptureResultAsync(enrolmentId, new ResultRequest
            {
                ResultStatus = resultStatus,
                FinishTime = finished ? finishTime : null,
                PositionOverall = finished ? positionOverall : null,
                PositionInCategory = finished ? positionInCategory : null
            });
            if (SessionExpired(result)) return ToLogin();

            if (result.Success) Success("Result captured.");
            else Error(result.Error);

            return RedirectToAction(nameof(Results), new { id });
        }

        // ---------- Helpers ----------
        private async Task LoadDisciplines(EventFormViewModel model)
        {
            var disciplines = await Api.GetDisciplinesAsync();
            model.Disciplines = disciplines.Data ?? new();
        }

        private async Task<EventDetail?> LoadOwnEvent(int id)
        {
            var ev = await Api.GetEventAsync(id);
            if (!ev.Success || ev.Data is null)
            {
                Error(ev.Error);
                return null;
            }

            if (ev.Data.OrganiserId != HttpContext.Session.GetInt32(SessionKeys.UserId))
            {
                Error("You can only manage events you created.");
                return null;
            }

            return ev.Data;
        }

        private async Task<CategoriesViewModel?> LoadCategoriesPage(int id)
        {
            var ev = await LoadOwnEvent(id);
            if (ev is null) return null;

            var categories = await Api.GetCategoriesAsync(id);
            return new CategoriesViewModel { Event = ev, Categories = categories.Data ?? new() };
        }

        private static EventRequest ToRequest(EventFormViewModel model, bool includeStatus) => new()
        {
            DisciplineId = model.DisciplineId,
            EventName = model.EventName,
            Description = model.Description,
            EventDate = model.EventDate,
            StartTime = model.StartTime,
            VenueName = model.VenueName,
            City = model.City,
            Province = model.Province,
            RegistrationCloseDate = model.RegistrationCloseDate,
            Status = includeStatus ? model.Status : null
        };
    }
}
