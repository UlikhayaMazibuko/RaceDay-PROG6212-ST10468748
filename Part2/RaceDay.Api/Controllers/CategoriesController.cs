using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers
{
   
    [ApiController]
    [Produces("application/json")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _db;

        public CategoriesController(RaceDayDbContext db)
        {
            _db = db;
        }

        [HttpGet("api/events/{eventId:int}/categories")]
        [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategoriesForEvent(int eventId)
        {
            if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            {
                return NotFound(new { message = "No event with that id." });
            }

            var categories = await ToResponses(_db.EventCategories.Where(c => c.EventId == eventId))
                .OrderByDescending(c => c.DistanceKm)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("api/categories/{id:int}")]
        [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await ToResponses(_db.EventCategories.Where(c => c.CategoryId == id))
                .FirstOrDefaultAsync();

            if (category is null)
            {
                return NotFound(new { message = "No category with that id." });
            }

            return Ok(category);
        }

        [HttpPost("api/events/{eventId:int}/categories")]
        [RequireRole(RoleNames.Organiser)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCategory(int eventId, CategoryRequest request)
        {
            var ev = await _db.Events.FindAsync(eventId);
            if (ev is null)
            {
                return NotFound(new { message = "No event with that id." });
            }

            if (ev.OrganiserId != HttpContext.GetCurrentUserId())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only add categories to events you created." });
            }

            var name = request.CategoryName.Trim();

            if (await _db.EventCategories.AnyAsync(c => c.EventId == eventId && c.CategoryName == name))
            {
                return Conflict(new { message = "This event already has a category with that name." });
            }

            var category = new EventCategory { EventId = eventId };
            CopyFields(request, category);

            _db.EventCategories.Add(category);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId },
                new { category.CategoryId, category.EventId, category.CategoryName });
        }

        [HttpPut("api/categories/{id:int}")]
        [RequireRole(RoleNames.Organiser)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
        {
            var category = await _db.EventCategories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category is null)
            {
                return NotFound(new { message = "No category with that id." });
            }

            if (category.Event!.OrganiserId != HttpContext.GetCurrentUserId())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only change categories on events you created." });
            }

            var activeEntries = await _db.Enrolments
                .CountAsync(en => en.CategoryId == id && en.EnrolmentStatus != "Cancelled");

            if (request.MaxParticipants.HasValue && request.MaxParticipants.Value < activeEntries)
            {
                return BadRequest(new { message = $"MaxParticipants cannot be lower than the {activeEntries} people already entered." });
            }

            var name = request.CategoryName.Trim();
            if (await _db.EventCategories.AnyAsync(c => c.EventId == category.EventId && c.CategoryName == name && c.CategoryId != id))
            {
                return Conflict(new { message = "This event already has a category with that name." });
            }

            CopyFields(request, category);
            await _db.SaveChangesAsync();

            return Ok(new { category.CategoryId, category.EventId, category.CategoryName });
        }

        [HttpDelete("api/categories/{id:int}")]
        [RequireRole(RoleNames.Organiser)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _db.EventCategories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category is null)
            {
                return NotFound(new { message = "No category with that id." });
            }

            if (category.Event!.OrganiserId != HttpContext.GetCurrentUserId())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete categories on events you created." });
            }

            if (await _db.Enrolments.AnyAsync(en => en.CategoryId == id))
            {
                return Conflict(new { message = "This category already has enrolments and cannot be deleted." });
            }

            _db.EventCategories.Remove(category);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private static void CopyFields(CategoryRequest request, EventCategory category)
        {
            category.CategoryName = request.CategoryName.Trim();
            category.DistanceKm = request.DistanceKm!.Value;
            category.EntryFee = request.EntryFee;
            category.CategoryStartTime = request.CategoryStartTime;
            category.MaxParticipants = request.MaxParticipants;
            category.MinimumAge = request.MinimumAge;
        }

        private static IQueryable<CategoryResponse> ToResponses(IQueryable<EventCategory> query)
        {
            return query.Select(c => new CategoryResponse
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                EventName = c.Event!.EventName,
                CategoryName = c.CategoryName,
                DistanceKm = c.DistanceKm,
                EntryFee = c.EntryFee,
                CategoryStartTime = c.CategoryStartTime,
                MaxParticipants = c.MaxParticipants,
                MinimumAge = c.MinimumAge,
                EnrolledCount = c.Enrolments.Count(en => en.EnrolmentStatus != "Cancelled"),
                PlacesLeft = c.MaxParticipants == null
                    ? null
                    : c.MaxParticipants - c.Enrolments.Count(en => en.EnrolmentStatus != "Cancelled")
            });
        }
    }
}