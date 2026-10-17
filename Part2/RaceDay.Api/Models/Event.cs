namespace RaceDay.Api.Models
{
    public class Event
    {
        public int EventId { get; set; }

        public int OrganiserId { get; set; }

        public int DisciplineId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateOnly EventDate { get; set; }

        public TimeOnly StartTime { get; set; }

        public string VenueName { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Province { get; set; } = string.Empty;

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public DateOnly RegistrationCloseDate { get; set; }

        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt { get; set; }

        public User? Organiser { get; set; }

        public Discipline? Discipline { get; set; }

        public ICollection<EventCategory> Categories { get; set; } = new List<EventCategory>();
    }
}