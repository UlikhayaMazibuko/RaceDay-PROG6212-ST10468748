namespace RaceDay.Api.Models
{
    public class Discipline
    {
        public int DisciplineId { get; set; }

        public string DisciplineName { get; set; } = string.Empty;

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}