using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Platform.Application.Identity;
using IAuthenticationService = Platform.Application.Identity.IAuthenticationService;

namespace Platform.Api.Identity;

public sealed class ManagerCookieEvents(IAuthenticationService authentication) : CookieAuthenticationEvents
{
    public const string Scheme = "Manager";
    public const string SessionClaim = "kipo:session";

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Guid.TryParse(context.Principal?.FindFirstValue(SessionClaim), out var sessionId)
            || await authentication.ValidateSessionAsync(sessionId, userId, context.HttpContext.RequestAborted) is null)
        {
            context.RejectPrincipal();
        }
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
