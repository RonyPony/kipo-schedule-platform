using System.ComponentModel.DataAnnotations;

namespace Platform.Application.Identity;

public sealed record LoginRequest(
    [Required(ErrorMessage = "Introduce tu correo."), EmailAddress(ErrorMessage = "Introduce un correo válido."), MaxLength(256, ErrorMessage = "El correo no puede superar los 256 caracteres.")] string Email,
    [Required(ErrorMessage = "Introduce tu contraseña."), MaxLength(256, ErrorMessage = "La contraseña no puede superar los 256 caracteres.")] string Password);

public sealed record CurrentUser(Guid Id, string DisplayName, string Email, string State, bool EmailConfirmed);

public sealed record CsrfToken(string Token, string HeaderName);

public sealed record LoginSession(Guid Id, Guid UserId, CurrentUser User, DateTimeOffset ExpiresAt);

public interface IAuthenticationService
{
    Task<LoginSession?> LoginAsync(LoginRequest request, string correlationId, CancellationToken cancellationToken);
    Task<CurrentUser?> ValidateSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task RevokeAsync(Guid sessionId, Guid userId, bool allSessions, string correlationId, CancellationToken cancellationToken);
}
