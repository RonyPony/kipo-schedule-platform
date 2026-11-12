using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;
using Platform.Application.Businesses;
using Platform.Domain.Auditing;
using Platform.Domain.Businesses;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Businesses;

public sealed class BusinessService(BusinessDbContext database, PlatformDbContext platform, BusinessContext context,
    IBusinessAccessService access) : IBusinessService
{
    public async Task<BusinessDetails?> GetAsync(CancellationToken cancellationToken)
    {
        var business = await database.Businesses.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return business is null ? null : Details(business);
    }

    public async Task<BusinessUpdateResult> UpdateAsync(UpdateBusinessRequest request, string correlationId, CancellationToken cancellationToken)
    {
        byte[] version;
        try { version = Convert.FromBase64String(request.RowVersion); }
        catch (FormatException) { return new(BusinessUpdateStatus.InvalidVersion); }
        if (version.Length != 8) return new(BusinessUpdateStatus.InvalidVersion);
        if (context.BusinessId is not Guid businessId || context.UserId is not Guid userId) return new(BusinessUpdateStatus.Forbidden);
        await using var transaction = await platform.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await database.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);
        var permission = await access.AuthorizeAsync(businessId, userId, true, cancellationToken);
        if (permission != BusinessAccessStatus.Allowed)
            return new(permission == BusinessAccessStatus.NotFound ? BusinessUpdateStatus.NotFound : BusinessUpdateStatus.Forbidden);
        var business = await database.Businesses.SingleOrDefaultAsync(cancellationToken);
        if (business is null) return new(BusinessUpdateStatus.NotFound);
        if (!business.RowVersion.SequenceEqual(version)) return new(BusinessUpdateStatus.Conflict);
        business.Name = request.Name.Trim();
        business.Activity = request.Activity.Trim();
        business.Phone = request.Phone.Trim();
        database.Entry(business).Property(item => item.RowVersion).OriginalValue = version;
        database.AuditEvents.Add(new AuditEvent
        {
            BusinessId = businessId,
            ActorUserId = userId,
            Action = "BusinessUpdated",
            ResourceType = "Business",
            ResourceId = businessId,
            CorrelationId = correlationId
        });
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(BusinessUpdateStatus.Updated, Details(business));
        }
        catch (DbUpdateConcurrencyException) { return new(BusinessUpdateStatus.Conflict); }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 1205 })
        {
            return new(BusinessUpdateStatus.Conflict);
        }
    }

    private BusinessDetails Details(Business business) => new(business.Id, business.Name, business.Activity, business.Phone,
        business.CountryCode, business.TimeZoneId, (int)business.WeekStartsOn, business.SlotMinutes,
        context.Role.ToString()!, Convert.ToBase64String(business.RowVersion));
}
