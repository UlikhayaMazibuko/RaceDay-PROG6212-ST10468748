using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{
    public class DisciplineResponse
    {
        public int DisciplineId { get; set; }
        public string DisciplineName { get; set; } = string.Empty;
    }

    public class EventSummaryResponse
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
    }

    public class EventCategoryInfo
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
    }

    public class EventDetailResponse
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
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public DateOnly RegistrationCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int OrganiserId { get; set; }
        public string OrganiserName { get; set; } = string.Empty;
        public List<EventCategoryInfo> Categories { get; set; } = new();
    }

    public class CreateEventRequest
    {
        [Required]
        public int? DisciplineId { get; set; }

        [Required, StringLength(120)]
        public string EventName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateOnly? EventDate { get; set; }

        [Required]
        public TimeOnly? StartTime { get; set; }

        [Required, StringLength(150)]
        public string VenueName { get; set; } = string.Empty;

        [Required, StringLength(60)]
        public string City { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Province { get; set; } = string.Empty;

        [Range(-90, 90)]
        public decimal? Latitude { get; set; }

        [Range(-180, 180)]
        public decimal? Longitude { get; set; }

        [Required]
        public DateOnly? RegistrationCloseDate { get; set; }
    }

    public class UpdateEventRequest : CreateEventRequest
    {
        [Required, RegularExpression("^(Draft|Published|Completed|Cancelled)$", ErrorMessage = "Status must be Draft, Published, Completed or Cancelled.")]
        public string Status { get; set; } = string.Empty;
    }
}