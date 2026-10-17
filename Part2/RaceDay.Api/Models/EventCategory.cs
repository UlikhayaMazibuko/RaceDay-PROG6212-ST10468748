namespace RaceDay.Api.Models
{
    public class EventCategory
    {
        public int CategoryId { get; set; }

        public int EventId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal DistanceKm { get; set; }

        public decimal EntryFee { get; set; }

        public TimeOnly? CategoryStartTime { get; set; }

        public int? MaxParticipants { get; set; }

        public int MinimumAge { get; set; }

        public Event? Event { get; set; }

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}