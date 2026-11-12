using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Identity;

public sealed class ProtectedUserStore(PlatformDbContext database, IDataProtectionProvider protection)
    : UserStore<ApplicationUser, IdentityRole<Guid>, PlatformDbContext, Guid>(database)
{
    public override Task SetTokenAsync(ApplicationUser user, string loginProvider, string name, string? value, CancellationToken cancellationToken = default)
    {
        if (Protect(loginProvider, name) && value is not null)
            value = protection.CreateProtector("Kipo.Identity.Mfa", user.Id.ToString(), name).Protect(value);
        return base.SetTokenAsync(user, loginProvider, name, value, cancellationToken);
    }

    public override async Task<string?> GetTokenAsync(ApplicationUser user, string loginProvider, string name, CancellationToken cancellationToken = default)
    {
        var value = await base.GetTokenAsync(user, loginProvider, name, cancellationToken);
        return Protect(loginProvider, name) && value is not null
            ? protection.CreateProtector("Kipo.Identity.Mfa", user.Id.ToString(), name).Unprotect(value)
            : value;
    }

    private static bool Protect(string provider, string name) => provider == "[AspNetUserStore]" && name is "AuthenticatorKey" or "RecoveryCodes";
}
