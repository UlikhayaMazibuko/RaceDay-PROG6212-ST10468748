namespace RaceDay.Web.Services
{
    // The names of the values we save in the session
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

    // These make it easy to check who is logged in and what their role is
    public static class SessionUserExtensions
    {
        public static bool IsLoggedIn(this ISession session) => session.GetInt32(SessionKeys.UserId).HasValue;

        public static string? GetRole(this ISession session) => session.GetString(SessionKeys.Role);

        public static bool IsOrganiser(this ISession session) => session.GetRole() == Roles.Organiser;

        public static bool IsParticipant(this ISession session) => session.GetRole() == Roles.Participant;

        public static string GetFullName(this ISession session) => session.GetString(SessionKeys.FullName) ?? string.Empty;
    }
}
