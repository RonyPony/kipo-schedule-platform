using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Platform.Application.Identity;

namespace Platform.Api.Identity;

public sealed class AdminCookieEvents(IAdminAuthenticationService authentication) : CookieAuthenticationEvents
{
    public const string Scheme = "Admin";
    public const string Policy = "SuperAdmin";
    public const string SessionClaim = "kipo:admin-session";

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Guid.TryParse(context.Principal?.FindFirstValue(SessionClaim), out var sessionId)
            || context.Principal?.FindFirstValue("amr") != "mfa"
            || await authentication.ValidateSessionAsync(sessionId, userId, context.HttpContext.RequestAborted) is null)
            context.RejectPrincipal();
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
