using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{

    public class EnrolmentResponse
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

    public class UpdateEnrolmentStatusRequest
    {
        [Required, RegularExpression("^(Pending|Confirmed|Cancelled)$", ErrorMessage = "EnrolmentStatus must be Pending, Confirmed or Cancelled.")]
        public string EnrolmentStatus { get; set; } = string.Empty;
    }
}