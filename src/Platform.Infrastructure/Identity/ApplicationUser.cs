using Microsoft.AspNetCore.Identity;
using Platform.Domain.Identity;

namespace Platform.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public UserState State { get; set; } = UserState.PendingApproval;
    public bool IsPlatformAdmin { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
