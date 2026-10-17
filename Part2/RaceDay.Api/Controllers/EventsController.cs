using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers
{

    [ApiController]
    [Route("api/events")]
    [Produces("application/json")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _db;

        public EventsController(RaceDayDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<EventSummaryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvents([FromQuery] string? city, [FromQuery] int? disciplineId, [FromQuery] DateOnly? fromDate)
        {
            var query = _db.Events
                .Where(e => e.Status == "Published" || e.Status == "Completed");

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(e => e.City == city);
            }

            if (disciplineId.HasValue)
            {
                query = query.Where(e => e.DisciplineId == disciplineId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(e => e.EventDate >= fromDate.Value);
            }

            var events = await ToSummaries(query.OrderBy(e => e.EventDate)).ToListAsync();
            return Ok(events);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _db.Events
                .Where(e => e.EventId == id)
                .Select(e => new EventDetailResponse
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    Description = e.Description,
                    DisciplineId = e.DisciplineId,
                    DisciplineName = e.Discipline!.DisciplineName,
                    EventDate = e.EventDate,
                    StartTime = e.StartTime,
                    VenueName = e.VenueName,
                    City = e.City,
                    Province = e.Province,
                    Latitude = e.Latitude,
                    Longitude = e.Longitude,
                    RegistrationCloseDate = e.RegistrationCloseDate,
                    Status = e.Status,
                    OrganiserId = e.OrganiserId,
                    OrganiserName = e.Organiser!.FirstName + " " + e.Organiser.LastName,
                    Categories = e.Categories
                        .OrderBy(c => c.DistanceKm)
                        .Select(c => new EventCategoryInfo
                        {
                            CategoryId = c.CategoryId,
                            CategoryName = c.CategoryName,
                            DistanceKm = c.DistanceKm,
                            EntryFee = c.EntryFee
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (ev is null)
            {
                return NotFound(new { message = "No event with that id." });
            }

            return Ok(ev);
        }

        [HttpGet("mine")]
        [RequireRole(RoleNames.Organiser)]
        [ProducesResponseType(typeof(List<EventSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEvents()
        {
            var organiserId = HttpContext.GetCurrentUserId()!.Value;

            var events = await ToSummaries(
                    _db.Events
                        .Where(e => e.OrganiserId == organiserId)
                        .OrderBy(e => e.EventDate))
                .ToListAsync();

            return Ok(events);
        }

        [HttpPost]
        [RequireRole(RoleNames.Organiser)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent(CreateEventRequest request)
        {
            var problem = await ValidateEventAsync(request);
            if (problem is not null)
            {
                return BadRequest(new { message = problem });
            }

            if (request.EventDate!.Value < DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new { message = "The event date cannot be in the past." });
            }

            var ev = new Event
            {
                OrganiserId = HttpContext.GetCurrentUserId()!.Value,

                Status = "Published"
            };
            CopyFields(request, ev);

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, new { ev.EventId, ev.EventName, ev.Status });
        }

        [HttpPut("{id:int}")]
        [RequireRole(RoleNames.Organiser)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, UpdateEventRequest request)
        {
            var ev = await _db.Events.FindAsync(id);
            if (ev is null)
            {
                return NotFound(new { message = "No event with that id." });
            }

            if (ev.OrganiserId != HttpContext.GetCurrentUserId())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only change events you created." });
            }

            var problem = await ValidateEventAsync(request);
            if (problem is not null)
            {
                return BadRequest(new { message = problem });
            }

            CopyFields(request, ev);
            ev.Status = request.Status;

            await _db.SaveChangesAsync();

            return Ok(new { ev.EventId, ev.EventName, ev.Status });
        }

        [HttpDelete("{id:int}")]
        [RequireRole(RoleNames.Organiser)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _db.Events.FindAsync(id);
            if (ev is null)
            {
                return NotFound(new { message = "No event with that id." });
            }

            if (ev.OrganiserId != HttpContext.GetCurrentUserId())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete events you created." });
            }

            var hasEnrolments = await _db.Enrolments.AnyAsync(en => en.Category!.EventId == id);
            if (hasEnrolments)
            {
                return Conflict(new { message = "This event already has enrolments and cannot be deleted. Set its status to Cancelled instead." });
            }

            _db.Events.Remove(ev);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private async Task<string?> ValidateEventAsync(CreateEventRequest request)
        {
            if (!await _db.Disciplines.AnyAsync(d => d.DisciplineId == request.DisciplineId))
            {
                return "DisciplineId must be 1 (Running), 2 (Walking) or 3 (Cycling).";
            }

            if (request.RegistrationCloseDate!.Value > request.EventDate!.Value)
            {
                return "Registration must close on or before the event date.";
            }

            return null;
        }

        private static void CopyFields(CreateEventRequest request, Event ev)
        {
            ev.DisciplineId = request.DisciplineId!.Value;
            ev.EventName = request.EventName.Trim();
            ev.Description = request.Description;
            ev.EventDate = request.EventDate!.Value;
            ev.StartTime = request.StartTime!.Value;
            ev.VenueName = request.VenueName.Trim();
            ev.City = request.City.Trim();
            ev.Province = request.Province.Trim();
            ev.Latitude = request.Latitude;
            ev.Longitude = request.Longitude;
            ev.RegistrationCloseDate = request.RegistrationCloseDate!.Value;
        }

  
        private static IQueryable<EventSummaryResponse> ToSummaries(IQueryable<Event> query)
        {
            return query.Select(e => new EventSummaryResponse
            {
                EventId = e.EventId,
                EventName = e.EventName,
                DisciplineName = e.Discipline!.DisciplineName,
                EventDate = e.EventDate,
                StartTime = e.StartTime,
                VenueName = e.VenueName,
                City = e.City,
                Province = e.Province,
                Status = e.Status,
                DistancesKm = e.Categories.OrderByDescending(c => c.DistanceKm).Select(c => c.DistanceKm).ToList(),
                EnrolmentCount = e.Categories.SelectMany(c => c.Enrolments).Count()
            });
        }
    }
}