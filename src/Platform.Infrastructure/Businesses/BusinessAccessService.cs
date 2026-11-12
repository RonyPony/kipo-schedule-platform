using Microsoft.EntityFrameworkCore;
using Platform.Application.Businesses;
using Platform.Domain.Businesses;
using Platform.Domain.Identity;
using Platform.Infrastructure.Persistence;

namespace Platform.Infrastructure.Businesses;

public sealed class BusinessAccessService(PlatformDbContext database, BusinessContext context) : IBusinessAccessService
{
    public async Task<BusinessAccessStatus> AuthorizeAsync(Guid businessId, Guid userId, bool ownerOnly, CancellationToken cancellationToken)
    {
        var access = await (from membership in database.BusinessMemberships.AsNoTracking()
                            join business in database.Businesses.AsNoTracking() on membership.BusinessId equals business.Id
                            join user in database.Users.AsNoTracking() on membership.UserId equals user.Id
                            where membership.BusinessId == businessId && membership.UserId == userId
                            select new { membership.State, membership.Role, BusinessState = business.State, UserState = user.State, user.EmailConfirmed, user.IsPlatformAdmin })
            .SingleOrDefaultAsync(cancellationToken);
        if (access is null || access.State == MembershipState.Revoked) return BusinessAccessStatus.NotFound;
        if (access.State != MembershipState.Active || access.BusinessState != BusinessState.Active
            || access.UserState != UserState.Active || !access.EmailConfirmed || access.IsPlatformAdmin
            || access.Role is not (BusinessRole.Owner or BusinessRole.Manager)
            || (ownerOnly && access.Role != BusinessRole.Owner)) return BusinessAccessStatus.Forbidden;
        context.Bind(businessId, userId, access.Role);
        return BusinessAccessStatus.Allowed;
    }
}
