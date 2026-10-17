using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers
{

    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthController(RaceDayDbContext db, IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            if (request.DateOfBirth!.Value >= DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new { message = "Date of birth must be in the past." });
            }

            var role = await _db.Roles.FindAsync(request.RoleId!.Value);
            if (role is null)
            {
                return BadRequest(new { message = "RoleId must be 1 (Organiser) or 2 (Participant)." });
            }

            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                return Conflict(new { message = "An account with this email already exists." });
            }

            var user = new User
            {
                RoleId = role.RoleId,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth.Value,
                Gender = request.Gender,
                City = request.City
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Created("/api/users/me", ToResponse(user, role.RoleName));
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user is null || !user.IsActive)
            {
                return Unauthorized(new { message = "Email or password is incorrect." });
            }

            var check = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (check == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Email or password is incorrect." });
            }

            if (check == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
                await _db.SaveChangesAsync();
            }

            HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
            HttpContext.Session.SetString(SessionKeys.RoleName, user.Role!.RoleName);

            return Ok(ToResponse(user, user.Role.RoleName));
        }

        [HttpPost("logout")]
        [RequireRole]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".RaceDay.Session");
            return NoContent();
        }

        [HttpGet("session")]
        [RequireRole]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSession()
        {
            var userId = HttpContext.GetCurrentUserId()!.Value;

            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user is null)
            {
                HttpContext.Session.Clear();
                return Unauthorized(new { message = "You must be logged in to use this endpoint." });
            }

            return Ok(ToResponse(user, user.Role!.RoleName));
        }

        private static AuthResponse ToResponse(User user, string roleName)
        {
            return new AuthResponse
            {
                UserId = user.UserId,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                RoleName = roleName
            };
        }
    }
}