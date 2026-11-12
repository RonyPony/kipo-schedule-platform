using Microsoft.EntityFrameworkCore;
using Platform.Application.Businesses;
using Platform.Domain.Auditing;
using Platform.Domain.Businesses;
using Platform.Domain.Notifications;
using Platform.Domain.Registration;

namespace Platform.Infrastructure.Persistence;

public sealed class BusinessDbContext(DbContextOptions<BusinessDbContext> options, BusinessContext context) : PlatformDbContext(options)
{
    private Guid? ActiveBusinessId => context.BusinessId;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Business>().HasQueryFilter(item => ActiveBusinessId.HasValue && item.Id == ActiveBusinessId);
        builder.Entity<BusinessMembership>().HasQueryFilter(item => ActiveBusinessId.HasValue && item.BusinessId == ActiveBusinessId);
        builder.Entity<RegistrationApplication>().HasQueryFilter(item => ActiveBusinessId.HasValue && item.BusinessId == ActiveBusinessId);
        builder.Entity<AuditEvent>().HasQueryFilter(item => ActiveBusinessId.HasValue && item.BusinessId == ActiveBusinessId);
        builder.Entity<OutboxMessage>().HasQueryFilter(item => ActiveBusinessId.HasValue && item.BusinessId == ActiveBusinessId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidateWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateWrites()
    {
        if (Database.CurrentTransaction is null || context.BusinessId is null || context.UserId is null || context.Role != BusinessRole.Owner)
            throw new InvalidOperationException("La escritura requiere un propietario y una transacción autorizados.");
        foreach (var entry in ChangeTracker.Entries().Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Entity is Business business && entry.State == EntityState.Modified
                && business.Id == context.BusinessId
                && entry.Properties.Where(property => property.IsModified).All(property => property.Metadata.Name is nameof(Business.Name) or nameof(Business.Activity) or nameof(Business.Phone))) continue;
            if (entry.Entity is AuditEvent audit && entry.State == EntityState.Added
                && audit.BusinessId == context.BusinessId && audit.ActorUserId == context.UserId) continue;
            throw new InvalidOperationException("La escritura no está permitida en este contexto de negocio.");
        }
    }
}
