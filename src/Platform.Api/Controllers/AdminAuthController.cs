using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Platform.Api.Identity;
using Platform.Application.Identity;

namespace Platform.Api.Controllers;

[ApiController]
[Route("api/v1/admin/auth")]
[ServiceFilter(typeof(CsrfValidationFilter))]
public sealed class AdminAuthController(IAdminAuthenticationService authentication, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf")]
    public ActionResult<CsrfToken> Csrf()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return new CsrfToken(tokens.RequestToken!, "X-CSRF-TOKEN");
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType<CurrentUser>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CurrentUser>> Login(AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var session = await authentication.LoginAsync(request, HttpContext.TraceIdentifier, cancellationToken);
        if (session is null)
        {
            var problem = new ProblemDetails { Status = 401, Title = "No se pudo iniciar sesión. Revisa tus credenciales y el código de seguridad." };
            problem.Extensions["code"] = "invalid_admin_credentials";
            problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
            return new ObjectResult(problem) { StatusCode = 401 };
        }
        if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var previousUser)
            && Guid.TryParse(User.FindFirstValue(AdminCookieEvents.SessionClaim), out var previousSession))
            await authentication.RevokeAsync(previousUser, previousSession, false, HttpContext.TraceIdentifier, cancellationToken);
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new Claim(AdminCookieEvents.SessionClaim, session.Id.ToString()),
            new Claim("amr", "mfa")
        ], AdminCookieEvents.Scheme);
        await HttpContext.SignInAsync(AdminCookieEvents.Scheme, new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = true
        });
        return session.User;
    }

    [HttpGet("me")]
    [Authorize(Policy = AdminCookieEvents.Policy)]
    [ProducesResponseType<CurrentUser>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUser>> Me(CancellationToken cancellationToken)
    {
        var user = await authentication.ValidateSessionAsync(SessionId, UserId, cancellationToken);
        return user is null ? Unauthorized() : user;
    }

    [HttpPost("logout")]
    [Authorize(Policy = AdminCookieEvents.Policy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Logout(CancellationToken cancellationToken) => Revoke(false, cancellationToken);

    [HttpPost("logout-all")]
    [Authorize(Policy = AdminCookieEvents.Policy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> LogoutAll(CancellationToken cancellationToken) => Revoke(true, cancellationToken);

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Guid SessionId => Guid.Parse(User.FindFirstValue(AdminCookieEvents.SessionClaim)!);

    private async Task<IActionResult> Revoke(bool allSessions, CancellationToken cancellationToken)
    {
        await authentication.RevokeAsync(UserId, SessionId, allSessions, HttpContext.TraceIdentifier, cancellationToken);
        await HttpContext.SignOutAsync(AdminCookieEvents.Scheme);
        return NoContent();
    }
}
