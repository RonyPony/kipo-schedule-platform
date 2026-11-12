using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Identity;
using Platform.Domain.Identity;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Identity;

public sealed class AdminAuthenticationService(UserManager<ApplicationUser> users, PlatformDbContext database, TimeProvider clock)
    : IAdminAuthenticationService
{
    private static readonly ApplicationUser UnknownUser = new();
    private static readonly PasswordHasher<ApplicationUser> Hasher = new();
    private static readonly string DummyHash = Hasher.HashPassword(UnknownUser, Guid.NewGuid().ToString());

    public async Task<LoginSession?> LoginAsync(AdminLoginRequest request, string correlationId, CancellationToken cancellationToken)
    {
        var userId = await database.Users.AsNoTracking().Where(user => user.NormalizedEmail == users.NormalizeEmail(request.Email.Trim()))
            .Select(user => (Guid?)user.Id).SingleOrDefaultAsync(cancellationToken);
        if (userId is null)
        {
            Hasher.VerifyHashedPassword(UnknownUser, DummyHash, request.Password);
            return null;
        }
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await LockAsync(database, userId.Value, cancellationToken);
        var user = await users.FindByIdAsync(userId.Value.ToString());
        if (user is null || !user.IsPlatformAdmin || user.State != UserState.Active || !user.EmailConfirmed
            || !user.TwoFactorEnabled || await users.IsLockedOutAsync(user)) return null;
        if (!await users.CheckPasswordAsync(user, request.Password)
            || !await VerifyMfaAsync(users, user, request.Code, request.UseRecoveryCode, clock.GetUtcNow()))
        {
            Require(await users.AccessFailedAsync(user));
            AddAudit(user.Id, "admin.login_failed", correlationId);
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }
        Require(await users.ResetAccessFailedCountAsync(user));
        var now = clock.GetUtcNow();
        if (request.UseRecoveryCode)
        {
            Require(await users.UpdateSecurityStampAsync(user));
            await database.UserSessions.Where(session => session.UserId == user.Id && session.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, now), cancellationToken);
        }
        var session = new UserSession
        {
            UserId = user.Id,
            IsAdministrative = true,
            SecurityStamp = user.SecurityStamp!,
            CreatedAt = now,
            ExpiresAt = now.AddHours(8)
        };
        database.UserSessions.Add(session);
        AddAudit(user.Id, request.UseRecoveryCode ? "admin.recovery_login" : "admin.login", correlationId);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(session.Id, user.Id, Describe(user), session.ExpiresAt);
    }

    public async Task<CurrentUser?> ValidateSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var user = await (from session in database.UserSessions.AsNoTracking()
                          join account in database.Users.AsNoTracking() on session.UserId equals account.Id
                          where session.Id == sessionId && session.UserId == userId && session.IsAdministrative
                              && session.RevokedAt == null && session.ExpiresAt > now && account.IsPlatformAdmin
                              && account.State == UserState.Active && account.EmailConfirmed && account.TwoFactorEnabled
                              && account.SecurityStamp == session.SecurityStamp
                          select account).SingleOrDefaultAsync(cancellationToken);
        return user is null ? null : Describe(user);
    }

    public async Task RevokeAsync(Guid userId, Guid sessionId, bool allSessions, string correlationId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await database.UserSessions.Where(session => session.UserId == userId && session.IsAdministrative && session.RevokedAt == null
            && (allSessions || session.Id == sessionId)).ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAt, now), cancellationToken);
        AddAudit(userId, allSessions ? "admin.logout_all" : "admin.logout", correlationId);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public static Task LockAsync(PlatformDbContext database, Guid userId, CancellationToken cancellationToken) =>
        database.Database.ExecuteSqlInterpolatedAsync($"DECLARE @result int; EXEC @result = sp_getapplock @Resource = {"kipo:admin:" + userId}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000; IF @result < 0 THROW 51000, 'Administrative authentication is busy.', 1;", cancellationToken);

    public static async Task<bool> VerifyMfaAsync(UserManager<ApplicationUser> users, ApplicationUser user, string suppliedCode, bool recovery, DateTimeOffset now)
    {
        var code = suppliedCode.Trim().Replace(" ", "");
        if (recovery) return (await users.RedeemTwoFactorRecoveryCodeAsync(user, code)).Succeeded;
        if (code.Length != 6 || code.Any(character => character is < '0' or > '9')) return false;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
        var used = await users.GetAuthenticationTokenAsync(user, "Kipo.Admin", "UsedTotp");
        var entries = (used ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Split(':')).Where(parts => parts.Length == 2
                && long.TryParse(parts[1], CultureInfo.InvariantCulture, out var expires) && expires > now.ToUnixTimeSeconds()).ToList();
        if (entries.Any(parts => parts[0] == hash)) return false;
        if (!await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, code)) return false;
        entries.Add([hash, now.AddMinutes(3).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)]);
        Require(await users.SetAuthenticationTokenAsync(user, "Kipo.Admin", "UsedTotp", string.Join(';', entries.Select(parts => string.Join(':', parts)))));
        return true;
    }

    public static void Require(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException("No se pudo actualizar la identidad administrativa.");
    }

    private void AddAudit(Guid userId, string action, string correlationId) => database.AuthenticationAudits.Add(new AuthenticationAudit
    {
        UserId = userId,
        Action = action,
        CorrelationId = correlationId,
        CreatedAt = clock.GetUtcNow()
    });

    private static CurrentUser Describe(ApplicationUser user) => new(user.Id, user.DisplayName, user.Email!, user.State.ToString(), user.EmailConfirmed);
}
