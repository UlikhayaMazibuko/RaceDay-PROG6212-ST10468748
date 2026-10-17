namespace RaceDay.Api.Models
{
    public class Enrolment
    {
        public int EnrolmentId { get; set; }

        public int CategoryId { get; set; }

        public int ParticipantId { get; set; }

        public string RaceNumber { get; set; } = string.Empty;

        public DateTime EnrolmentDate { get; set; }

        public string EnrolmentStatus { get; set; } = "Pending";

        public EventCategory? Category { get; set; }

        public User? Participant { get; set; }

        public Result? Result { get; set; }
    }
}