using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers
{
   
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _db;

        public EnrolmentsController(RaceDayDbContext db)
        {
            _db = db;
        }

        [HttpPost("api/categories/{categoryId:int}/enrolments")]
        [RequireRole(RoleNames.Participant)]
        public async Task<IActionResult> Enrol(int categoryId)
        {
            var participantId = HttpContext.GetCurrentUserId()!.Value;

            var category = await _db.EventCategories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

            if (category is null)
            {
                return NotFound(new { message = "No category with that id." });
            }

            var ev = category.Event!;
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (ev.Status != "Published")
            {
                return Conflict(new { message = "This event is not open for entries." });
            }

            if (today > ev.RegistrationCloseDate)
            {
                return Conflict(new { message = "Entries for this event have closed." });
            }

            if (await _db.Enrolments.AnyAsync(en => en.CategoryId == categoryId && en.ParticipantId == participantId))
            {
                return Conflict(new { message = "You have already entered this category." });
            }

            var activeEntries = await _db.Enrolments
                .CountAsync(en => en.CategoryId == categoryId && en.EnrolmentStatus != "Cancelled");

            if (category.MaxParticipants.HasValue && activeEntries >= category.MaxParticipants.Value)
            {
                return Conflict(new { message = "This category is full." });
            }

            var participant = await _db.Users.FindAsync(participantId);
            if (AgeOn(participant!.DateOfBirth, ev.EventDate) < category.MinimumAge)
            {
                return Conflict(new { message = $"You must be at least {category.MinimumAge} on race day to enter this category." });
            }

            var enrolment = new Enrolment
            {
                CategoryId = categoryId,
                ParticipantId = participantId,
                RaceNumber = await NextRaceNumberAsync(categoryId),
                EnrolmentStatus = "Pending"
            };

            _db.Enrolments.Add(enrolment);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEnrolment), new { id = enrolment.EnrolmentId },
                new { enrolment.EnrolmentId, enrolment.RaceNumber, enrolment.EnrolmentStatus });
        }

        [HttpGet("api/enrolments/mine")]
        [RequireRole(RoleNames.Participant)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var participantId = HttpContext.GetCurrentUserId()!.Value;

            var enrolments = await ToResponses(_db.Enrolments.Where(en => en.ParticipantId == participantId))
                .OrderBy(en => en.EventDate)
                .ToListAsync();

            return Ok(enrolments);
        }

        [HttpGet("api/enrolments/{id:int}")]
        [RequireRole]
        public async Task<IActionResult> GetEnrolment(int id)
        {
            var enrolment = await ToResponses(_db.Enrolments.Where(en => en.EnrolmentId == id))
                .FirstOrDefaultAsync();

            if (enrolment is null)
            {
                return NotFound(new { message = "No enrolment with that id." });
            }

            var userId = HttpContext.GetCurrentUserId();
            var organiserId = await _db.Events
                .Where(e => e.EventId == enrolment.EventId)
                .Select(e => e.OrganiserId)
                .FirstAsync();

            if (enrolment.ParticipantId != userId && organiserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only view your own enrolments or enrolments for events you organise." });
            }

            return Ok(enrolment);
        }

        [HttpGet("api/events/{eventId:int}/enrolments")]
        [RequireRole(RoleNames.Organiser)]
        public async Task<IActionResult> GetEnrolmentsForEvent(int eventId)
        {
            var ev = await _db.Events.FindAsync(eventId);
            if (ev is null)
            {
                return NotFound(new { message = "No event with that id." });
            }

            if (ev.OrganiserId != HttpContext.GetCurrentUserId())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only view enrolments for events you created." });
            }

            var enrolments = await ToResponses(_db.Enrolments.Where(en => en.Category!.EventId == eventId))
                .OrderBy(en => en.CategoryName)
                .ThenBy(en => en.RaceNumber)
                .ToListAsync();

            return Ok(enrolments);
        }

        [HttpPut("api/enrolments/{id:int}/status")]
        [RequireRole(RoleNames.Organiser)]
        public async Task<IActionResult> UpdateStatus(int id, UpdateEnrolmentStatusRequest request)
        {
            var enrolment = await _db.Enrolments
                .Include(en => en.Category)
                    .ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(en => en.EnrolmentId == id);

            if (enrolment is null)
            {
                return NotFound(new { message = "No enrolment with that id." });
            }

            if (enrolment.Category!.Event!.OrganiserId != HttpContext.GetCurrentUserId())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only change enrolments for events you created." });
            }

            enrolment.EnrolmentStatus = request.EnrolmentStatus;
            await _db.SaveChangesAsync();

            return Ok(new { enrolment.EnrolmentId, enrolment.RaceNumber, enrolment.EnrolmentStatus });
        }

        [HttpDelete("api/enrolments/{id:int}")]
        [RequireRole(RoleNames.Participant)]
        public async Task<IActionResult> Withdraw(int id)
        {
            var enrolment = await _db.Enrolments
                .Include(en => en.Category)
                    .ThenInclude(c => c!.Event)
                .Include(en => en.Result)
                .FirstOrDefaultAsync(en => en.EnrolmentId == id);

            if (enrolment is null)
            {
                return NotFound(new { message = "No enrolment with that id." });
            }

            if (enrolment.ParticipantId != HttpContext.GetCurrentUserId())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only withdraw your own enrolments." });
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (enrolment.Result is not null || enrolment.Category!.Event!.EventDate <= today)
            {
                return Conflict(new { message = "You cannot withdraw once the event has taken place or a result has been captured." });
            }

            _db.Enrolments.Remove(enrolment);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private static int AgeOn(DateOnly dateOfBirth, DateOnly onDate)
        {
            var age = onDate.Year - dateOfBirth.Year;
            if (dateOfBirth > onDate.AddYears(-age))
            {
                age--;
            }
            return age;
        }

        private async Task<string> NextRaceNumberAsync(int categoryId)
        {
            var next = await _db.Enrolments.CountAsync(en => en.CategoryId == categoryId) + 1;
            string candidate;

            do
            {
                candidate = $"C{categoryId}-{next:D4}";
                next++;
            }
            while (await _db.Enrolments.AnyAsync(en => en.CategoryId == categoryId && en.RaceNumber == candidate));

            return candidate;
        }

        private static IQueryable<EnrolmentResponse> ToResponses(IQueryable<Enrolment> query)
        {
            return query.Select(en => new EnrolmentResponse
            {
                EnrolmentId = en.EnrolmentId,
                RaceNumber = en.RaceNumber,
                EnrolmentStatus = en.EnrolmentStatus,
                EnrolmentDate = en.EnrolmentDate,
                EventId = en.Category!.EventId,
                EventName = en.Category.Event!.EventName,
                EventDate = en.Category.Event.EventDate,
                CategoryId = en.CategoryId,
                CategoryName = en.Category.CategoryName,
                DistanceKm = en.Category.DistanceKm,
                ParticipantId = en.ParticipantId,
                ParticipantName = en.Participant!.FirstName + " " + en.Participant.LastName,
                HasResult = en.Result != null
            });
        }
    }
}