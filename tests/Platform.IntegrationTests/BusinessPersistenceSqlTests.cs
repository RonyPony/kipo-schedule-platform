using Microsoft.EntityFrameworkCore;
using Platform.Domain.Auditing;
using Platform.Domain.Businesses;
using Platform.Domain.Notifications;
using Platform.Domain.Registration;
using Platform.Infrastructure.Identity;
using Platform.Infrastructure.Persistence;

namespace Platform.IntegrationTests;

public sealed class BusinessPersistenceSqlTests(AuthenticationSqlFixture fixture) : IClassFixture<AuthenticationSqlFixture>
{
    [SqlFact]
    public async Task RegistrationGraphAndOutboxCommitTogetherWithPendingStates()
    {
        await using var database = new PlatformDbContext(fixture.Options);
        var application = AddRegistration(database);
        await database.SaveChangesAsync();
        await using var verification = new PlatformDbContext(fixture.Options);
        Assert.Equal(RegistrationState.PendingReview, (await verification.RegistrationApplications.FindAsync(application.Id))!.State);
        var business = (await verification.Businesses.FindAsync(application.BusinessId))!;
        Assert.Equal(BusinessState.PendingApproval, business.State);
        Assert.Equal(8, business.RowVersion.Length);
        var membership = (await verification.BusinessMemberships.FindAsync(application.BusinessId, application.ApplicantUserId))!;
        Assert.Equal(MembershipState.Pending, membership.State);
        Assert.Equal(BusinessRole.Owner, membership.Role);
        Assert.Single(await verification.OutboxMessages.Where(message => message.BusinessId == business.Id).ToListAsync());
        Assert.Single(await verification.AuditEvents.Where(audit => audit.BusinessId == business.Id).ToListAsync());
    }

    [SqlFact]
    public async Task DuplicateOutboxKeyRollsBackTheEntireNewRegistration()
    {
        await using var database = new PlatformDbContext(fixture.Options);
        var original = AddRegistration(database);
        await database.SaveChangesAsync();
        var key = await database.OutboxMessages.Where(message => message.BusinessId == original.BusinessId).Select(message => message.DeduplicationKey).SingleAsync();
        var duplicate = AddRegistration(database, key);
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
        await using var verification = new PlatformDbContext(fixture.Options);
        Assert.Null(await verification.Users.FindAsync(duplicate.ApplicantUserId));
        Assert.Null(await verification.Businesses.FindAsync(duplicate.BusinessId));
        Assert.Null(await verification.RegistrationApplications.FindAsync(duplicate.Id));
        Assert.False(await verification.AuditEvents.AnyAsync(audit => audit.BusinessId == duplicate.BusinessId));
    }

    [SqlFact]
    public async Task MembershipIsUniquePerBusinessAndUser()
    {
        await using var database = new PlatformDbContext(fixture.Options);
        var application = AddRegistration(database);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        database.BusinessMemberships.Add(new BusinessMembership { BusinessId = application.BusinessId, UserId = application.ApplicantUserId });
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }

    [SqlFact]
    public async Task ApplicationCannotReferenceAnApplicantFromAnotherBusiness()
    {
        await using var database = new PlatformDbContext(fixture.Options);
        var application = AddRegistration(database);
        var foreignUser = new ApplicationUser { Id = Guid.NewGuid(), DisplayName = "Otro solicitante" };
        database.Users.Add(foreignUser);
        await database.SaveChangesAsync();
        application.ApplicantUserId = foreignUser.Id;
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
        await using var verification = new PlatformDbContext(fixture.Options);
        Assert.NotEqual(foreignUser.Id, (await verification.RegistrationApplications.FindAsync(application.Id))!.ApplicantUserId);
    }

    [SqlFact]
    public async Task ConcurrentBusinessUpdatesCannotOverwriteEachOther()
    {
        await using var database = new PlatformDbContext(fixture.Options);
        var application = AddRegistration(database);
        await database.SaveChangesAsync();
        await using var first = new PlatformDbContext(fixture.Options);
        await using var second = new PlatformDbContext(fixture.Options);
        var firstBusiness = (await first.Businesses.FindAsync(application.BusinessId))!;
        var secondBusiness = (await second.Businesses.FindAsync(application.BusinessId))!;
        firstBusiness.Name = "Primer cambio";
        secondBusiness.Name = "Segundo cambio";
        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [SqlFact]
    public async Task ConcurrentApplicationDecisionsAllowOnlyOneWinner()
    {
        await using var database = new PlatformDbContext(fixture.Options);
        var application = AddRegistration(database);
        var reviewer = new ApplicationUser { Id = Guid.NewGuid(), DisplayName = "Revisor", IsPlatformAdmin = true };
        database.Users.Add(reviewer);
        await database.SaveChangesAsync();
        await using var first = new PlatformDbContext(fixture.Options);
        await using var second = new PlatformDbContext(fixture.Options);
        var approval = (await first.RegistrationApplications.FindAsync(application.Id))!;
        var rejection = (await second.RegistrationApplications.FindAsync(application.Id))!;
        approval.State = RegistrationState.Approved;
        approval.ReviewedBy = reviewer.Id;
        approval.ReviewedAt = DateTimeOffset.UtcNow;
        rejection.State = RegistrationState.Rejected;
        rejection.ReviewedBy = reviewer.Id;
        rejection.ReviewedAt = DateTimeOffset.UtcNow;
        rejection.PublicReason = "Información insuficiente.";
        var results = await Task.WhenAll(TrySave(first), TrySave(second));
        Assert.Single(results, succeeded => succeeded);
        Assert.Single(results, succeeded => !succeeded);
    }

    [SqlFact]
    public async Task BusinessWithHistoryCannotBeDeleted()
    {
        await using var database = new PlatformDbContext(fixture.Options);
        var application = AddRegistration(database);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        database.Businesses.Remove((await database.Businesses.FindAsync(application.BusinessId))!);
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }

    [SqlFact]
    public async Task InvalidPlanningGranularityIsRejectedBySql()
    {
        await using var database = new PlatformDbContext(fixture.Options);
        database.Businesses.Add(new Business { Name = "Configuración inválida", CountryCode = "DO", SlotMinutes = 17 });
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }

    [SqlFact]
    public async Task OutboxLeaseCannotBeClaimedByTwoContexts()
    {
        await using var database = new PlatformDbContext(fixture.Options);
        var application = AddRegistration(database);
        await database.SaveChangesAsync();
        await using var first = new PlatformDbContext(fixture.Options);
        await using var second = new PlatformDbContext(fixture.Options);
        foreach (var context in new[] { first, second })
        {
            var message = await context.OutboxMessages.SingleAsync(item => item.BusinessId == application.BusinessId);
            message.State = OutboxState.Processing;
            message.LeaseId = Guid.NewGuid();
            message.LeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(2);
        }
        var results = await Task.WhenAll(TrySave(first), TrySave(second));
        Assert.Single(results, succeeded => succeeded);
        Assert.Single(results, succeeded => !succeeded);
    }

    [SqlFact]
    public async Task ProcessingOutboxMessageRequiresLeaseMetadata()
    {
        await using var database = new PlatformDbContext(fixture.Options);
        var application = AddRegistration(database);
        await database.SaveChangesAsync();
        var message = await database.OutboxMessages.SingleAsync(item => item.BusinessId == application.BusinessId);
        message.State = OutboxState.Processing;
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }

    [SqlFact]
    public async Task RejectionWithoutPublicReasonCannotBePersisted()
    {
        await using var database = new PlatformDbContext(fixture.Options);
        var application = AddRegistration(database);
        var reviewer = new ApplicationUser { Id = Guid.NewGuid(), DisplayName = "Revisor", IsPlatformAdmin = true };
        database.Users.Add(reviewer);
        await database.SaveChangesAsync();
        application.State = RegistrationState.Rejected;
        application.ReviewedBy = reviewer.Id;
        application.ReviewedAt = DateTimeOffset.UtcNow;
        application.PublicReason = "   ";
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }

    [SqlFact]
    public async Task OutboxPayloadMustBeBoundedValidJson()
    {
        await using var database = new PlatformDbContext(fixture.Options);
        var application = AddRegistration(database);
        await database.SaveChangesAsync();
        var message = await database.OutboxMessages.SingleAsync(item => item.BusinessId == application.BusinessId);
        message.Payload = "invalid";
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
        message.Payload = "{\"value\":\"" + new string('a', 8000) + "\"}";
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }

    private static async Task<bool> TrySave(PlatformDbContext database)
    {
        try
        {
            await database.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private static RegistrationApplication AddRegistration(PlatformDbContext database, string? deduplicationKey = null)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), DisplayName = "Solicitante de prueba" };
        var business = new Business { Name = "Negocio de prueba", Activity = "Servicios", CountryCode = "DO" };
        var application = new RegistrationApplication { BusinessId = business.Id, ApplicantUserId = user.Id };
        database.Users.Add(user);
        database.Businesses.Add(business);
        database.BusinessMemberships.Add(new BusinessMembership { BusinessId = business.Id, UserId = user.Id, Role = BusinessRole.Owner });
        database.RegistrationApplications.Add(application);
        database.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = user.Id,
            BusinessId = business.Id,
            Action = "RegistrationSubmitted",
            ResourceType = "RegistrationApplication",
            ResourceId = application.Id,
            CorrelationId = Guid.NewGuid().ToString()
        });
        database.OutboxMessages.Add(new OutboxMessage
        {
            BusinessId = business.Id,
            Type = "RegistrationReceived",
            Recipient = "recipient@example.test",
            DeduplicationKey = deduplicationKey ?? application.Id.ToString()
        });
        return application;
    }
}
