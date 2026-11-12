using System.ComponentModel.DataAnnotations;
using Platform.Domain.Businesses;

namespace Platform.Application.Businesses;

public sealed record BusinessDetails(Guid Id, string Name, string Activity, string Phone, string CountryCode,
    string TimeZoneId, int WeekStartsOn, int SlotMinutes, string Role, string RowVersion);

public sealed record UpdateBusinessRequest(
    [Required(ErrorMessage = "Introduce el nombre del negocio."), MaxLength(160, ErrorMessage = "El nombre admite hasta 160 caracteres.")] string Name,
    [Required(ErrorMessage = "Introduce la actividad del negocio."), MaxLength(160, ErrorMessage = "La actividad admite hasta 160 caracteres.")] string Activity,
    [Required(ErrorMessage = "Introduce el teléfono del negocio."), MaxLength(32, ErrorMessage = "El teléfono admite hasta 32 caracteres.")] string Phone,
    [Required(ErrorMessage = "Falta la versión del negocio."), MaxLength(16, ErrorMessage = "La versión del negocio no es válida.")] string RowVersion);

public enum BusinessAccessStatus { Allowed, NotFound, Forbidden }
public enum BusinessUpdateStatus { Updated, NotFound, Forbidden, Conflict, InvalidVersion }
public sealed record BusinessUpdateResult(BusinessUpdateStatus Status, BusinessDetails? Business = null);

public sealed class BusinessContext
{
    public Guid? BusinessId { get; private set; }
    public Guid? UserId { get; private set; }
    public BusinessRole? Role { get; private set; }

    public void Bind(Guid businessId, Guid userId, BusinessRole role)
    {
        if (BusinessId.HasValue && (BusinessId != businessId || UserId != userId))
            throw new InvalidOperationException("No se puede cambiar el negocio durante una operación.");
        BusinessId = businessId;
        UserId = userId;
        Role = role;
    }
}

public interface IBusinessAccessService
{
    Task<BusinessAccessStatus> AuthorizeAsync(Guid businessId, Guid userId, bool ownerOnly, CancellationToken cancellationToken);
}

public interface IBusinessService
{
    Task<BusinessDetails?> GetAsync(CancellationToken cancellationToken);
    Task<BusinessUpdateResult> UpdateAsync(UpdateBusinessRequest request, string correlationId, CancellationToken cancellationToken);
}
