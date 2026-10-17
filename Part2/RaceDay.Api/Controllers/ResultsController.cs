using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers
{

    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _db;

        public ResultsController(RaceDayDbContext db)
        {
            _db = db;
        }

        [HttpPost("api/enrolments/{enrolmentId:int}/result")]
        [RequireRole(RoleNames.Organiser)]
        public async Task<IActionResult> CaptureResult(int enrolmentId, ResultRequest request)
        {
            var enrolment = await _db.Enrolments
                .Include(en => en.Category)
                    .ThenInclude(c => c!.Event)
                .Include(en => en.Result)
                .FirstOrDefaultAsync(en => en.EnrolmentId == enrolmentId);

            if (enrolment is null)
            {
                return NotFound(new { message = "No enrolment with that id." });
            }

            var ev = enrolment.Category!.Event!;
            if (ev.OrganiserId != HttpContext.GetCurrentUserId())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only capture results for events you organise." });
            }

            if (ev.EventDate > DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new { message = "Results can only be captured once the event has taken place." });
            }

            if (enrolment.Result is not null)
            {
                return Conflict(new { message = "A result has already been captured for this enrolment. Use PUT /api/results/{id} to correct it." });
            }

            var problem = CheckResultFields(request);
            if (problem is not null)
            {
                return BadRequest(new { message = problem });
            }

            var result = new Result
            {
                EnrolmentId = enrolmentId,
                CapturedByUserId = HttpContext.GetCurrentUserId()!.Value
            };
            CopyFields(request, result);

            _db.Results.Add(result);
            await _db.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new { result.ResultId, result.EnrolmentId, result.ResultStatus });
        }

        [HttpPut("api/results/{id:int}")]
        [RequireRole(RoleNames.Organiser)]
        public async Task<IActionResult> CorrectResult(int id, ResultRequest request)
        {
            var result = await _db.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(en => en!.Category)
                        .ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result is null)
            {
                return NotFound(new { message = "No result with that id." });
            }

            if (result.Enrolment!.Category!.Event!.OrganiserId != HttpContext.GetCurrentUserId())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only correct results for events you organise." });
            }

            var problem = CheckResultFields(request);
            if (problem is not null)
            {
                return BadRequest(new { message = problem });
            }

            CopyFields(request, result);
            result.CapturedByUserId = HttpContext.GetCurrentUserId()!.Value;
            result.CapturedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { result.ResultId, result.EnrolmentId, result.ResultStatus });
        }

        [HttpGet("api/results/mine")]
        [RequireRole(RoleNames.Participant)]
        public async Task<IActionResult> GetMyResults()
        {
            var participantId = HttpContext.GetCurrentUserId()!.Value;

            var results = await ToResponses(_db.Results.Where(r => r.Enrolment!.ParticipantId == participantId))
                .OrderByDescending(r => r.EventDate)
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("api/events/{eventId:int}/results")]
        public async Task<IActionResult> GetResultsForEvent(int eventId)
        {
            if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            {
                return NotFound(new { message = "No event with that id." });
            }

            var results = await ToResponses(_db.Results.Where(r => r.Enrolment!.Category!.EventId == eventId))
                .OrderBy(r => r.PositionOverall == null)
                .ThenBy(r => r.PositionOverall)
                .ToListAsync();

            return Ok(results);
        }

        private static string? CheckResultFields(ResultRequest request)
        {
            if (request.ResultStatus == "Finished")
            {
                if (request.FinishTime is null || request.PositionOverall is null || request.PositionInCategory is null)
                {
                    return "A finished result needs FinishTime, PositionOverall and PositionInCategory.";
                }
            }
            else if (request.FinishTime is not null || request.PositionOverall is not null || request.PositionInCategory is not null)
            {
                return $"A {request.ResultStatus} result must not have a finish time or positions.";
            }

            return null;
        }

        private static void CopyFields(ResultRequest request, Result result)
        {
            result.FinishTime = request.FinishTime;
            result.PositionOverall = request.PositionOverall;
            result.PositionInCategory = request.PositionInCategory;
            result.ResultStatus = request.ResultStatus;
        }

        private IQueryable<ResultResponse> ToResponses(IQueryable<Result> query)
        {
            return query.Select(r => new ResultResponse
            {
                ResultId = r.ResultId,
                EnrolmentId = r.EnrolmentId,
                RaceNumber = r.Enrolment!.RaceNumber,
                EventId = r.Enrolment.Category!.EventId,
                EventName = r.Enrolment.Category.Event!.EventName,
                EventDate = r.Enrolment.Category.Event.EventDate,
                CategoryName = r.Enrolment.Category.CategoryName,
                DistanceKm = r.Enrolment.Category.DistanceKm,
                ParticipantName = r.Enrolment.Participant!.FirstName + " " + r.Enrolment.Participant.LastName,
                FinishTime = r.FinishTime,
                PositionOverall = r.PositionOverall,
                PositionInCategory = r.PositionInCategory,
                ResultStatus = r.ResultStatus,
                TotalFinishers = _db.Results.Count(other =>
                    other.Enrolment!.Category!.EventId == r.Enrolment.Category.EventId
                    && other.ResultStatus == "Finished")
            });
        }
    }
}