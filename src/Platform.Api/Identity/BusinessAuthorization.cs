using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Platform.Application.Businesses;

namespace Platform.Api.Identity;

public sealed record BusinessRequirement(bool OwnerOnly) : IAuthorizationRequirement;

public sealed class BusinessAuthorizationHandler(IBusinessAccessService access) : AuthorizationHandler<BusinessRequirement>
{
    internal static readonly object HiddenResource = new();
    public const string MemberPolicy = "BusinessMember";
    public const string OwnerPolicy = "BusinessOwner";

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, BusinessRequirement requirement)
    {
        if (context.Resource is not HttpContext http || context.User.Identity?.IsAuthenticated != true) return;
        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Guid.TryParse(http.Request.RouteValues["businessId"]?.ToString(), out var businessId)) return;
        var result = await access.AuthorizeAsync(businessId, userId, requirement.OwnerOnly, http.RequestAborted);
        if (result == BusinessAccessStatus.Allowed) context.Succeed(requirement);
        else if (result == BusinessAccessStatus.NotFound) http.Items[HiddenResource] = true;
    }
}

public sealed class BusinessAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler fallback = new();

    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        if (result.Forbidden && context.Items.ContainsKey(BusinessAuthorizationHandler.HiddenResource))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }
        return fallback.HandleAsync(next, context, policy, result);
    }
}
