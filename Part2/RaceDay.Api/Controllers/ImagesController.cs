using Azure;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{
    /// <summary>
    /// Image uploads for event banners and profile pictures. Files are stored in Azure Blob Storage
    /// and the public URL is saved in the database. The MVC site never talks to Azure directly.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class ImagesController : ControllerBase
    {
        private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
        private const long MaxBytes = 5 * 1024 * 1024;

        private readonly RaceDayDbContext _db;
        private readonly IImageStorage _storage;
        private readonly ILogger<ImagesController> _logger;

        public ImagesController(RaceDayDbContext db, IImageStorage storage, ILogger<ImagesController> logger)
        {
            _db = db;
            _storage = storage;
            _logger = logger;
        }

        /// <summary>
        /// Uploads a banner image for one of the logged in organiser's events.
        /// </summary>
        /// <remarks>
        /// Send the image as multipart/form-data in a field called File (JPG, PNG, WEBP or GIF, up to 5 MB).
        /// The image is stored in Azure Blob Storage and its URL is saved on the event, replacing any
        /// previous banner. The URL is returned as bannerImageUrl on the event endpoints.
        /// </remarks>
        /// <param name="eventId">The event id.</param>
        /// <param name="request">The image file.</param>
        /// <response code="200">The banner was uploaded. Returns its public URL.</response>
        /// <response code="400">No file, the wrong file type, or a file over 5 MB.</response>
        /// <response code="401">Nobody is logged in.</response>
        /// <response code="403">The caller is a participant, or did not create this event.</response>
        /// <response code="404">No event with that id.</response>
        /// <response code="503">Azure Blob Storage is not configured or could not be reached.</response>
        [HttpPost("api/events/{eventId:int}/banner")]
        [RequireRole(RoleNames.Organiser)]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UploadEventBanner(int eventId, [FromForm] ImageUploadRequest request)
        {
            var ev = await _db.Events.FindAsync(eventId);
            if (ev is null)
            {
                return NotFound(new { message = "No event with that id." });
            }

            if (ev.OrganiserId != HttpContext.GetCurrentUserId())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only change the banner of events you created." });
            }

            var problem = CheckFile(request.File);
            if (problem is not null)
            {
                return BadRequest(new { message = problem });
            }

            try
            {
                ev.BannerImageUrl = await _storage.UploadAsync(request.File!, "event-banners");
                await _db.SaveChangesAsync();
            }
            catch (Exception ex) when (ex is InvalidOperationException or RequestFailedException)
            {
                _logger.LogError(ex, "Banner upload to Azure Blob Storage failed.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "The image could not be stored right now. Please try again later." });
            }

            return Ok(new ImageUploadResponse { Url = ev.BannerImageUrl ?? string.Empty });
        }

        /// <summary>
        /// Uploads a profile picture for the logged in user.
        /// </summary>
        /// <remarks>
        /// Available to both roles. Send the image as multipart/form-data in a field called File
        /// (JPG, PNG, WEBP or GIF, up to 5 MB). The URL is returned as profilePictureUrl on GET /api/users/me.
        /// </remarks>
        /// <param name="request">The image file.</param>
        /// <response code="200">The picture was uploaded. Returns its public URL.</response>
        /// <response code="400">No file, the wrong file type, or a file over 5 MB.</response>
        /// <response code="401">Nobody is logged in.</response>
        /// <response code="503">Azure Blob Storage is not configured or could not be reached.</response>
        [HttpPost("api/users/me/picture")]
        [RequireRole]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UploadProfilePicture([FromForm] ImageUploadRequest request)
        {
            var userId = HttpContext.GetCurrentUserId();
            var user = userId is null ? null : await _db.Users.FindAsync(userId.Value);
            if (user is null)
            {
                return Unauthorized(new { message = "You must be logged in to use this endpoint." });
            }

            var problem = CheckFile(request.File);
            if (problem is not null)
            {
                return BadRequest(new { message = problem });
            }

            try
            {
                user.ProfilePictureUrl = await _storage.UploadAsync(request.File!, "profile-pictures");
                await _db.SaveChangesAsync();
            }
            catch (Exception ex) when (ex is InvalidOperationException or RequestFailedException)
            {
                _logger.LogError(ex, "Profile picture upload to Azure Blob Storage failed.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "The image could not be stored right now. Please try again later." });
            }

            return Ok(new ImageUploadResponse { Url = user.ProfilePictureUrl ?? string.Empty });
        }

        private static string? CheckFile(IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                return "Please choose an image to upload.";
            }

            if (!AllowedTypes.Contains(file.ContentType))
            {
                return "Only JPG, PNG, WEBP and GIF images are allowed.";
            }

            if (file.Length > MaxBytes)
            {
                return "Images must be 5 MB or smaller.";
            }

            return null;
        }
    }
}
