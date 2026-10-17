using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{
    public class CategoryRequest
    {
        [Required, StringLength(60)]
        public string CategoryName { get; set; } = string.Empty;

        [Required, Range(0.1, 999.99)]
        public decimal? DistanceKm { get; set; }

        [Range(0, 99999.99)]
        public decimal EntryFee { get; set; }

        public TimeOnly? CategoryStartTime { get; set; }

        [Range(1, 100000)]
        public int? MaxParticipants { get; set; }

        [Range(0, 120)]
        public int MinimumAge { get; set; }
    }
    public class CategoryResponse
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
}