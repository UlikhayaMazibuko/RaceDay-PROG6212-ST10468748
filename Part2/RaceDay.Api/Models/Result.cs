namespace RaceDay.Api.Models
{
    public class Result
    {
        public int ResultId { get; set; }

        public int EnrolmentId { get; set; }

        public int CapturedByUserId { get; set; }

        public TimeOnly? FinishTime { get; set; }

        public int? PositionOverall { get; set; }

        public int? PositionInCategory { get; set; }

        public string ResultStatus { get; set; } = "Finished";

        public DateTime CapturedAt { get; set; }

        public Enrolment? Enrolment { get; set; }

        public User? CapturedBy { get; set; }
    }
}