using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Infrastructure;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class SessionKeys
{
    public const string Token = "rd.token";
    public const string UserId = "rd.userId";
    public const string FullName = "rd.fullName";
    public const string Email = "rd.email";
    public const string Role = "rd.role";
}

public static class SessionExtensions
{
    public static void SignIn(this ISession session, AuthResponse auth)
    {
        session.Clear();
        session.SetString(SessionKeys.Token, auth.Token);
        session.SetString(SessionKeys.UserId, auth.UserId.ToString());
        session.SetString(SessionKeys.FullName, auth.FullName ?? auth.Email ?? "");
        session.SetString(SessionKeys.Email, auth.Email ?? "");
        session.SetString(SessionKeys.Role, NormaliseRole(auth.Role));
    }

    public static bool IsSignedIn(this ISession session) =>
        !string.IsNullOrEmpty(session.GetString(SessionKeys.Token));

    public static string? CurrentRole(this ISession session) => session.GetString(SessionKeys.Role);

    public static string CurrentName(this ISession session) => session.GetString(SessionKeys.FullName) ?? "";

    public static bool IsOrganiser(this ISession session) =>
        string.Equals(session.CurrentRole(), Roles.Organiser, StringComparison.OrdinalIgnoreCase);

    private static string NormaliseRole(string? role) =>
        string.Equals(role, Roles.Organiser, StringComparison.OrdinalIgnoreCase) ? Roles.Organiser : Roles.Participant;
}
