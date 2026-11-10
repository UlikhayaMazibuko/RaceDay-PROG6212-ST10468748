namespace RaceDay.Web.Services
{
    // Names of the values the website keeps in its own session.
    public static class SessionKeys
    {
        public const string UserId = "UserId";
        public const string Role = "Role";
        public const string FullName = "FullName";
        public const string ApiCookie = "ApiCookie";
    }

    public static class Roles
    {
        public const string Organiser = "Organiser";
        public const string Participant = "Participant";
    }

    // Small helpers so controllers and views can ask "who is logged in?" in one line.
    public static class SessionUserExtensions
    {
        public static bool IsLoggedIn(this ISession session) => session.GetInt32(SessionKeys.UserId).HasValue;

        public static string? GetRole(this ISession session) => session.GetString(SessionKeys.Role);

        public static bool IsOrganiser(this ISession session) => session.GetRole() == Roles.Organiser;

        public static bool IsParticipant(this ISession session) => session.GetRole() == Roles.Participant;

        public static string GetFullName(this ISession session) => session.GetString(SessionKeys.FullName) ?? string.Empty;
    }
}
