using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{
    public class UserProfileResponse
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? City { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileRequest
    {
        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Phone, StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(60)]
        public string? City { get; set; }
    }

  
    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one letter and one number.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}