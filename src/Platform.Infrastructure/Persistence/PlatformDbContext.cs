using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Platform.Infrastructure.Identity;
using Platform.Domain.Auditing;
using Platform.Domain.Businesses;
using Platform.Domain.Notifications;
using Platform.Domain.Registration;

namespace Platform.Infrastructure.Persistence;

public class PlatformDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options) { }
    protected PlatformDbContext(DbContextOptions options) : base(options) { }

    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AuthenticationAudit> AuthenticationAudits => Set<AuthenticationAudit>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessMembership> BusinessMemberships => Set<BusinessMembership>();
    public DbSet<RegistrationApplication> RegistrationApplications => Set<RegistrationApplication>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigureBusinessModel();
        builder.Entity<IdentityUserLogin<Guid>>().Property(login => login.LoginProvider).HasMaxLength(128);
        builder.Entity<IdentityUserLogin<Guid>>().Property(login => login.ProviderKey).HasMaxLength(128);
        builder.Entity<IdentityUserToken<Guid>>().Property(token => token.LoginProvider).HasMaxLength(128);
        builder.Entity<IdentityUserToken<Guid>>().Property(token => token.Name).HasMaxLength(128);
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(user => user.State).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(user => user.NormalizedEmail).IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
        });
        builder.Entity<UserSession>(entity =>
        {
            entity.HasKey(session => session.Id);
            entity.Property(session => session.SecurityStamp).HasMaxLength(256).IsRequired();
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(session => session.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(session => new { session.UserId, session.RevokedAt, session.ExpiresAt });
        });
        builder.Entity<AuthenticationAudit>(entity =>
        {
            entity.HasKey(audit => audit.Id);
            entity.Property(audit => audit.Action).HasMaxLength(64).IsRequired();
            entity.Property(audit => audit.CorrelationId).HasMaxLength(64).IsRequired();
            entity.HasIndex(audit => audit.CreatedAt);
        });
    }
}
