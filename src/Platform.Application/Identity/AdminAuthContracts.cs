using System.ComponentModel.DataAnnotations;

namespace Platform.Application.Identity;

public sealed record AdminLoginRequest(
    [Required(ErrorMessage = "Introduce tu correo."), EmailAddress(ErrorMessage = "Introduce un correo válido."), MaxLength(256, ErrorMessage = "El correo admite hasta 256 caracteres.")] string Email,
    [Required(ErrorMessage = "Introduce tu contraseña."), MaxLength(256, ErrorMessage = "La contraseña admite hasta 256 caracteres.")] string Password,
    [Required(ErrorMessage = "Introduce el código del autenticador o un código de recuperación."), MaxLength(64, ErrorMessage = "El código admite hasta 64 caracteres.")] string Code,
    bool UseRecoveryCode = false);

public interface IAdminAuthenticationService
{
    Task<LoginSession?> LoginAsync(AdminLoginRequest request, string correlationId, CancellationToken cancellationToken);
    Task<CurrentUser?> ValidateSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task RevokeAsync(Guid userId, Guid sessionId, bool allSessions, string correlationId, CancellationToken cancellationToken);
}
