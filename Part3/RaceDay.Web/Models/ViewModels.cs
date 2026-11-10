using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    // Shapes used by the pages and forms. The validation attributes give quick, friendly
    // feedback on the form; the API still checks everything again before saving.

    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required, StringLength(50), Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50), Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one letter and one number.")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone, Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [Required, Display(Name = "Date of birth")]
        public DateOnly? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? City { get; set; }

        [Required(ErrorMessage = "Please choose whether you are an Organiser or a Participant."), Display(Name = "I am a")]
        public int? RoleId { get; set; }
    }

    public class HomeViewModel
    {
        public List<EventSummary> Events { get; set; } = new();
        public List<Discipline> Disciplines { get; set; } = new();
        public string? City { get; set; }
        public int? DisciplineId { get; set; }
        public DateOnly? FromDate { get; set; }
    }

    public class EventDetailsViewModel
    {
        public EventDetail Event { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<RaceResult> Results { get; set; } = new();
        public HashSet<int> MyCategoryIds { get; set; } = new();
        public bool EntriesOpen { get; set; }
    }

    public class DashboardViewModel
    {
        public List<EventSummary> Events { get; set; } = new();
        public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
        public List<EventSummary> Upcoming { get; set; } = new();
    }

    public class EventFormViewModel
    {
        public int? EventId { get; set; }

        [Required(ErrorMessage = "Please choose the event type."), Display(Name = "Event type")]
        public int? DisciplineId { get; set; }

        [Required, StringLength(120), Display(Name = "Event name")]
        public string EventName { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required, Display(Name = "Event date")]
        public DateOnly? EventDate { get; set; }

        [Required, Display(Name = "Start time")]
        public TimeOnly? StartTime { get; set; }

        [Required, StringLength(150), Display(Name = "Venue")]
        public string VenueName { get; set; } = string.Empty;

        [Required, StringLength(60)]
        public string City { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Province { get; set; } = string.Empty;

        [Required, Display(Name = "Entries close on")]
        public DateOnly? RegistrationCloseDate { get; set; }

        // Edit only.
        public string Status { get; set; } = "Published";

        // Create only: the event's first distance, so a new event is never left without one.
        [Display(Name = "First category name")]
        public string? FirstCategoryName { get; set; }

        [Range(0.1, 999.99), Display(Name = "Distance (km)")]
        public decimal? DistanceKm { get; set; }

        [Range(0, 99999.99), Display(Name = "Entry fee (R)")]
        public decimal EntryFee { get; set; }

        [Display(Name = "Banner image")]
        public IFormFile? Banner { get; set; }

        public string? CurrentBannerUrl { get; set; }

        public List<Discipline> Disciplines { get; set; } = new();
    }

    public class CategoryFormViewModel
    {
        [Required, StringLength(60), Display(Name = "Category name")]
        public string CategoryName { get; set; } = string.Empty;

        [Required, Range(0.1, 999.99), Display(Name = "Distance (km)")]
        public decimal? DistanceKm { get; set; }

        [Range(0, 99999.99), Display(Name = "Entry fee (R)")]
        public decimal EntryFee { get; set; }

        [Range(1, 100000), Display(Name = "Max participants")]
        public int? MaxParticipants { get; set; }

        [Range(0, 120), Display(Name = "Minimum age")]
        public int MinimumAge { get; set; }

        [Display(Name = "Start time")]
        public TimeOnly? CategoryStartTime { get; set; }
    }

    public class CategoriesViewModel
    {
        public EventDetail Event { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public CategoryFormViewModel NewCategory { get; set; } = new();
    }

    public class EnrolmentsViewModel
    {
        public EventDetail Event { get; set; } = new();
        public List<Enrolment> Enrolments { get; set; } = new();
    }

    public class ResultsEntryViewModel
    {
        public EventDetail Event { get; set; } = new();
        public List<Enrolment> Enrolments { get; set; } = new();
        public Dictionary<int, RaceResult> ResultsByEnrolment { get; set; } = new();
        public bool EventHasHappened { get; set; }
    }

    public class ProfileViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }

        [Required, StringLength(50), Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50), Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Phone, StringLength(20), Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(60)]
        public string? City { get; set; }
    }

    public class ErrorViewModel
    {
        public int? StatusCode { get; set; }
        public string Message { get; set; } = "Something went wrong on our side. Please try again.";
    }
}
