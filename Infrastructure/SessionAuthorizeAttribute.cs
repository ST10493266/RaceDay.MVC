using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Mvc.Infrastructure;

/// <summary>
/// Blocks unauthenticated users (redirect to login) and users with the wrong role (access denied).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string? Role { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        http.Response.Headers.CacheControl = "no-store"; // back button after logout must not show cached pages

        if (!http.Session.IsSignedIn())
        {
            var returnUrl = http.Request.Path + http.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (Role != null && !string.Equals(http.Session.CurrentRole(), Role, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
