namespace RaceDay.Web.Services
{
    // This class has small helpers the views use so the colours and wording are the same on every page
    public static class Ui
    {
        // Picks the badge colour for each status, green for Confirmed and amber for Pending
        public static string BadgeClass(string? status) => status switch
        {
            "Confirmed" or "Published" or "Finished" => "badge-status badge-green",
            "Pending" or "Draft" => "badge-status badge-amber",
            "Cancelled" or "DQ" => "badge-status badge-red",
            "Completed" => "badge-status badge-navy",
            _ => "badge-status badge-grey"
        };

        // Turns a position into 1st, 2nd, 3rd and so on (11, 12 and 13 always end in th)
        public static string Ordinal(int number)
        {
            var lastTwo = number % 100;
            if (lastTwo is >= 11 and <= 13) return $"{number}th";

            return (number % 10) switch
            {
                1 => $"{number}st",
                2 => $"{number}nd",
                3 => $"{number}rd",
                _ => $"{number}th"
            };
        }

        public static string Distances(IEnumerable<decimal> distances)
        {
            var list = distances.Select(d => $"{d:0.##} km").ToList();
            return list.Count == 0 ? "Distances to be announced" : string.Join(" · ", list);
        }

        public static string Rand(decimal amount) => amount == 0 ? "Free" : $"R{amount:0.00}";

        public static readonly string[] Provinces =
        {
            "Eastern Cape", "Free State", "Gauteng", "KwaZulu-Natal", "Limpopo",
            "Mpumalanga", "North West", "Northern Cape", "Western Cape"
        };
    }
}
