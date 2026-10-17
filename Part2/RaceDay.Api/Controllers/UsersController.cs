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
    [Route("api/users")]
    [RequireRole]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UsersController(RaceDayDbContext db, IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var user = await FindCurrentUserAsync();
            if (user is null)
            {
                return Unauthorized(new { message = "You must be logged in to use this endpoint." });
            }

            return Ok(ToProfile(user));
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(UpdateProfileRequest request)
        {
            var user = await FindCurrentUserAsync();
            if (user is null)
            {
                return Unauthorized(new { message = "You must be logged in to use this endpoint." });
            }

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.PhoneNumber = request.PhoneNumber;
            user.Gender = request.Gender;
            user.City = request.City;

            await _db.SaveChangesAsync();

            return Ok(ToProfile(user));
        }

        [HttpPut("me/password")]
        public async Task<IActionResult> ChangeMyPassword(ChangePasswordRequest request)
        {
            var user = await FindCurrentUserAsync();
            if (user is null)
            {
                return Unauthorized(new { message = "You must be logged in to use this endpoint." });
            }

            var check = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (check == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Your current password is incorrect." });
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private async Task<User?> FindCurrentUserAsync()
        {
            var userId = HttpContext.GetCurrentUserId();
            if (userId is null)
            {
                return null;
            }

            return await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);
        }

        private static UserProfileResponse ToProfile(User user)
        {
            return new UserProfileResponse
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                City = user.City,
                RoleName = user.Role?.RoleName ?? string.Empty,
                CreatedAt = user.CreatedAt
            };
        }
    }
}