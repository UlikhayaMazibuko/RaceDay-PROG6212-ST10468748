using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{
    public class ResultRequest
    {
        public TimeOnly? FinishTime { get; set; }

        [Range(1, 100000)]
        public int? PositionOverall { get; set; }

        [Range(1, 100000)]
        public int? PositionInCategory { get; set; }

        [Required, RegularExpression("^(Finished|DNF|DNS|DQ)$", ErrorMessage = "ResultStatus must be Finished, DNF, DNS or DQ.")]
        public string ResultStatus { get; set; } = "Finished";
    }

    public class ResultResponse
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