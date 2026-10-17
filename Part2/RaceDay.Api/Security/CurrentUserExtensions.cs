namespace RaceDay.Api.Security
{
    public static class CurrentUserExtensions
    {
        public static int? GetCurrentUserId(this HttpContext context)
        {
            return context.Session.GetInt32(SessionKeys.UserId);
        }

        public static string? GetCurrentRole(this HttpContext context)
        {
            return context.Session.GetString(SessionKeys.RoleName);
        }
    }
}