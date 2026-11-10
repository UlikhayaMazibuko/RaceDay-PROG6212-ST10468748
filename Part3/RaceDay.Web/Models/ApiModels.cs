namespace RaceDay.Web.Models
{
    // These classes mirror the JSON the Part 2 API sends and receives. They are data shapes only;
    // every rule (who can do what, capacity, dates) is enforced by the API, not here.

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? City { get; set; }
        public int? RoleId { get; set; }
    }

    public class AuthResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class UserProfile
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
        public string? ProfilePictureUrl { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public string? City { get; set; }
    }

    public class ImageUploadResponse
    {
        public string Url { get; set; } = string.Empty;
    }

    public class Discipline
    {
        public int DisciplineId { get; set; }
        public string DisciplineName { get; set; } = string.Empty;
    }

    public class EventSummary
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string DisciplineName { get; set; } = string.Empty;
        public DateOnly EventDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<decimal> DistancesKm { get; set; } = new();
        public int EnrolmentCount { get; set; }
        public string? BannerImageUrl { get; set; }
    }

    public class EventCategoryInfo
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
    }

    public class EventDetail
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisciplineId { get; set; }
        public string DisciplineName { get; set; } = string.Empty;
        public DateOnly EventDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public DateOnly RegistrationCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int OrganiserId { get; set; }
        public string OrganiserName { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public List<EventCategoryInfo> Categories { get; set; } = new();
    }

    public class EventRequest
    {
        public int? DisciplineId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateOnly? EventDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public DateOnly? RegistrationCloseDate { get; set; }

        // Only sent when updating. Left null on create, where the API publishes the event.
        public string? Status { get; set; }
    }

    public class CreatedEvent
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class Category
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
        public TimeOnly? CategoryStartTime { get; set; }
        public int? MaxParticipants { get; set; }
        public int MinimumAge { get; set; }
        public int EnrolledCount { get; set; }
        public int? PlacesLeft { get; set; }
    }

    public class CategoryRequest
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal? DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
        public TimeOnly? CategoryStartTime { get; set; }
        public int? MaxParticipants { get; set; }
        public int MinimumAge { get; set; }
    }

    public class NewEnrolment
    {
        public int EnrolmentId { get; set; }
        public string RaceNumber { get; set; } = string.Empty;
        public string EnrolmentStatus { get; set; } = string.Empty;
    }

    public class Enrolment
    {
        public int EnrolmentId { get; set; }
        public string RaceNumber { get; set; } = string.Empty;
        public string EnrolmentStatus { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateOnly EventDate { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public bool HasResult { get; set; }
    }

    public class ResultRequest
    {
        public TimeOnly? FinishTime { get; set; }
        public int? PositionOverall { get; set; }
        public int? PositionInCategory { get; set; }
        public string ResultStatus { get; set; } = "Finished";
    }

    public class RaceResult
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public string RaceNumber { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateOnly EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public TimeOnly? FinishTime { get; set; }
        public int? PositionOverall { get; set; }
        public int? PositionInCategory { get; set; }
        public string ResultStatus { get; set; } = string.Empty;
        public int TotalFinishers { get; set; }
    }
}
