using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Identity;
using Platform.Domain.Identity;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Identity;

public sealed class AuthenticationService(
    UserManager<ApplicationUser> users, PlatformDbContext database, TimeProvider clock) : IAuthenticationService
{
    private static readonly ApplicationUser UnknownUser = new();
    private static readonly PasswordHasher<ApplicationUser> DummyHasher = new();
    private static readonly string DummyHash = DummyHasher.HashPassword(UnknownUser, Guid.NewGuid().ToString());

    public async Task<LoginSession?> LoginAsync(LoginRequest request, string correlationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            DummyHasher.VerifyHashedPassword(UnknownUser, DummyHash, request.Password);
            return null;
        }
        if (await users.IsLockedOutAsync(user) || user.State == UserState.Disabled || user.IsPlatformAdmin)
        {
            return null;
        }
        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            return null;
        }
        if (!(await users.ResetAccessFailedCountAsync(user)).Succeeded)
        {
            return null;
        }
        var now = clock.GetUtcNow();
        var session = new UserSession
        {
            UserId = user.Id,
            SecurityStamp = user.SecurityStamp ?? string.Empty,
            CreatedAt = now,
            ExpiresAt = now.AddHours(12)
        };
        database.UserSessions.Add(session);
        database.AuthenticationAudits.Add(new AuthenticationAudit
        {
            UserId = user.Id,
            Action = "manager.login",
            CorrelationId = correlationId,
            CreatedAt = now
        });
        await database.SaveChangesAsync(cancellationToken);
        return new LoginSession(session.Id, user.Id, Describe(user), session.ExpiresAt);
    }

    public async Task<CurrentUser?> ValidateSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var session = await database.UserSessions.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == sessionId && item.UserId == userId && item.RevokedAt == null && item.ExpiresAt > now,
            cancellationToken);
        if (session is null) return null;
        var user = await database.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null || user.State == UserState.Disabled || user.IsPlatformAdmin || user.SecurityStamp != session.SecurityStamp)
        {
            return null;
        }
        return Describe(user);
    }

    public async Task RevokeAsync(Guid sessionId, Guid userId, bool allSessions, string correlationId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await database.UserSessions.Where(session => session.UserId == userId && session.RevokedAt == null && (allSessions || session.Id == sessionId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, now), cancellationToken);
        database.AuthenticationAudits.Add(new AuthenticationAudit
        {
            UserId = userId,
            Action = allSessions ? "manager.logout_all" : "manager.logout",
            CorrelationId = correlationId,
            CreatedAt = now
        });
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static CurrentUser Describe(ApplicationUser user) =>
        new(user.Id, user.DisplayName, user.Email ?? string.Empty, user.State.ToString(), user.EmailConfirmed);
}
