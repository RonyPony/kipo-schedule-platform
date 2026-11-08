using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Platform.Api.Identity;
using Platform.Application.Identity;
using IAuthenticationService = Platform.Application.Identity.IAuthenticationService;

namespace Platform.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[ServiceFilter(typeof(CsrfValidationFilter))]
public sealed class AuthController(IAuthenticationService authentication, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf")]
    [ProducesResponseType<CsrfToken>(StatusCodes.Status200OK)]
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
    public async Task<ActionResult<CurrentUser>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var session = await authentication.LoginAsync(request, HttpContext.TraceIdentifier, cancellationToken);
        if (session is null)
        {
            return Failure(401, "invalid_credentials", "No se pudo iniciar sesión. Revisa tus credenciales o inténtalo más tarde.");
        }
        if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var oldUserId)
            && Guid.TryParse(User.FindFirstValue(ManagerCookieEvents.SessionClaim), out var oldSessionId))
        {
            await authentication.RevokeAsync(oldSessionId, oldUserId, false, HttpContext.TraceIdentifier, cancellationToken);
        }
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new Claim(ManagerCookieEvents.SessionClaim, session.Id.ToString())
        ], ManagerCookieEvents.Scheme);
        await HttpContext.SignInAsync(ManagerCookieEvents.Scheme, new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = true
        });
        return session.User;
    }

    [Authorize(AuthenticationSchemes = ManagerCookieEvents.Scheme)]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUser>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUser>> Me(CancellationToken cancellationToken)
    {
        var user = await authentication.ValidateSessionAsync(SessionId, UserId, cancellationToken);
        return user is null ? Failure(401, "session_expired", "Tu sesión ha vencido. Inicia sesión de nuevo.") : user;
    }

    [Authorize(AuthenticationSchemes = ManagerCookieEvents.Scheme)]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Logout(CancellationToken cancellationToken) => Revoke(false, cancellationToken);

    [Authorize(AuthenticationSchemes = ManagerCookieEvents.Scheme)]
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> LogoutAll(CancellationToken cancellationToken) => Revoke(true, cancellationToken);

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Guid SessionId => Guid.Parse(User.FindFirstValue(ManagerCookieEvents.SessionClaim)!);

    private async Task<IActionResult> Revoke(bool allSessions, CancellationToken cancellationToken)
    {
        await authentication.RevokeAsync(SessionId, UserId, allSessions, HttpContext.TraceIdentifier, cancellationToken);
        await HttpContext.SignOutAsync(ManagerCookieEvents.Scheme);
        return NoContent();
    }

    private ObjectResult Failure(int status, string code, string title)
    {
        var problem = new ProblemDetails { Status = status, Title = title };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return new ObjectResult(problem) { StatusCode = status };
    }
}
