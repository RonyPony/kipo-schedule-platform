using Microsoft.EntityFrameworkCore;
using Platform.Domain.Auditing;
using Platform.Domain.Businesses;
using Platform.Domain.Notifications;
using Platform.Domain.Registration;
using Platform.Infrastructure.Identity;

namespace Platform.Infrastructure.Persistence;

internal static class BusinessModelConfiguration
{
    public static void ConfigureBusinessModel(this ModelBuilder builder)
    {
        builder.Entity<Business>(entity =>
        {
            entity.HasKey(business => business.Id);
            entity.Property(business => business.Name).HasMaxLength(160).IsRequired();
            entity.Property(business => business.Activity).HasMaxLength(160).IsRequired();
            entity.Property(business => business.Phone).HasMaxLength(32).IsRequired();
            entity.Property(business => business.CountryCode).HasMaxLength(2).IsUnicode(false).IsRequired();
            entity.Property(business => business.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.Property(business => business.State).HasConversion<string>().HasMaxLength(32);
            entity.Property(business => business.RowVersion).IsRowVersion();
            entity.HasIndex(business => new { business.State, business.CreatedAt });
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Businesses_SlotMinutes", "[SlotMinutes] IN (15, 30, 60)");
                table.HasCheckConstraint("CK_Businesses_WeekStartsOn", "[WeekStartsOn] BETWEEN 0 AND 6");
                table.HasCheckConstraint("CK_Businesses_State", "[State] IN ('PendingApproval', 'Active', 'Rejected', 'Suspended', 'Archived')");
            });
        });
        builder.Entity<BusinessMembership>(entity =>
        {
            entity.HasKey(membership => new { membership.BusinessId, membership.UserId });
            entity.Property(membership => membership.Role).HasConversion<string>().HasMaxLength(16);
            entity.Property(membership => membership.State).HasConversion<string>().HasMaxLength(16);
            entity.Property(membership => membership.RowVersion).IsRowVersion();
            entity.HasOne<Business>().WithMany().HasForeignKey(membership => membership.BusinessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(membership => membership.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(membership => new { membership.UserId, membership.State, membership.BusinessId });
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_BusinessMemberships_Role", "[Role] IN ('Owner', 'Manager')");
                table.HasCheckConstraint("CK_BusinessMemberships_State", "[State] IN ('Pending', 'Active', 'Revoked')");
                table.HasCheckConstraint("CK_BusinessMemberships_RevokedAt", "([State] = 'Revoked' AND [RevokedAt] IS NOT NULL) OR ([State] <> 'Revoked' AND [RevokedAt] IS NULL)");
            });
        });
        builder.Entity<RegistrationApplication>(entity =>
        {
            entity.HasKey(application => application.Id);
            entity.HasIndex(application => application.BusinessId).IsUnique();
            entity.HasIndex(application => application.ApplicantUserId).IsUnique();
            entity.HasIndex(application => new { application.State, application.CreatedAt });
            entity.Property(application => application.State).HasConversion<string>().HasMaxLength(32);
            entity.Property(application => application.PublicReason).HasMaxLength(1000);
            entity.Property(application => application.InternalNote).HasMaxLength(2000);
            entity.Property(application => application.RowVersion).IsRowVersion();
            entity.HasOne<BusinessMembership>().WithMany()
                .HasForeignKey(application => new { application.BusinessId, UserId = application.ApplicantUserId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(application => application.ReviewedBy).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_RegistrationApplications_State", "[State] IN ('PendingReview', 'Approved', 'Rejected')");
                table.HasCheckConstraint("CK_RegistrationApplications_Review", "([State] = 'PendingReview' AND [ReviewedBy] IS NULL AND [ReviewedAt] IS NULL) OR ([State] IN ('Approved', 'Rejected') AND [ReviewedBy] IS NOT NULL AND [ReviewedAt] IS NOT NULL)");
                table.HasCheckConstraint("CK_RegistrationApplications_RejectionReason", "[State] <> 'Rejected' OR ([PublicReason] IS NOT NULL AND LEN(LTRIM(RTRIM([PublicReason]))) > 0)");
            });
        });
        builder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(audit => audit.Id);
            entity.Property(audit => audit.Action).HasMaxLength(64).IsRequired();
            entity.Property(audit => audit.ResourceType).HasMaxLength(64).IsRequired();
            entity.Property(audit => audit.CorrelationId).HasMaxLength(64).IsRequired();
            entity.HasOne<Business>().WithMany().HasForeignKey(audit => audit.BusinessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(audit => audit.ActorUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(audit => new { audit.BusinessId, audit.CreatedAt });
            entity.HasIndex(audit => audit.CreatedAt);
        });
        builder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Type).HasMaxLength(64).IsRequired();
            entity.Property(message => message.Payload).HasMaxLength(8000).IsRequired();
            entity.Property(message => message.Recipient).HasMaxLength(256).IsRequired();
            entity.Property(message => message.DeduplicationKey).HasMaxLength(200).IsRequired();
            entity.Property(message => message.State).HasConversion<string>().HasMaxLength(16);
            entity.Property(message => message.LastErrorCode).HasMaxLength(64);
            entity.Property(message => message.RowVersion).IsRowVersion();
            entity.HasIndex(message => message.DeduplicationKey).IsUnique();
            entity.HasIndex(message => new { message.State, message.NextAttemptAt });
            entity.HasIndex(message => new { message.State, message.LeaseExpiresAt });
            entity.HasOne<Business>().WithMany().HasForeignKey(message => message.BusinessId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_OutboxMessages_State", "[State] IN ('Pending', 'Processing', 'Sent', 'Failed')");
                table.HasCheckConstraint("CK_OutboxMessages_Attempts", "[Attempts] >= 0");
                table.HasCheckConstraint("CK_OutboxMessages_Payload", "ISJSON([Payload]) = 1 AND DATALENGTH([Payload]) <= 16000");
                table.HasCheckConstraint("CK_OutboxMessages_Lease", "([State] = 'Processing' AND [LeaseId] IS NOT NULL AND [LeaseExpiresAt] IS NOT NULL) OR ([State] <> 'Processing' AND [LeaseId] IS NULL AND [LeaseExpiresAt] IS NULL)");
                table.HasCheckConstraint("CK_OutboxMessages_SentAt", "([State] = 'Sent' AND [SentAt] IS NOT NULL) OR ([State] <> 'Sent' AND [SentAt] IS NULL)");
            });
        });
    }
}
