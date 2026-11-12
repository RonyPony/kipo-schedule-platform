using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Api.Identity;
using Platform.Application.Businesses;

namespace Platform.Api.Controllers;

[ApiController]
[Route("api/v1/businesses/{businessId:guid}")]
[ServiceFilter(typeof(CsrfValidationFilter))]
public sealed class BusinessesController(IBusinessService businesses) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = BusinessAuthorizationHandler.MemberPolicy)]
    [ProducesResponseType<BusinessDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessDetails>> Get(CancellationToken cancellationToken)
    {
        var business = await businesses.GetAsync(cancellationToken);
        return business is null ? NotFound() : business;
    }

    [HttpPatch]
    [Authorize(Policy = BusinessAuthorizationHandler.OwnerPolicy)]
    [ProducesResponseType<BusinessDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BusinessDetails>> Update(UpdateBusinessRequest request, CancellationToken cancellationToken)
    {
        var result = await businesses.UpdateAsync(request, HttpContext.TraceIdentifier, cancellationToken);
        return result.Status switch
        {
            BusinessUpdateStatus.Updated => result.Business!,
            BusinessUpdateStatus.NotFound => NotFound(),
            BusinessUpdateStatus.Forbidden => Forbid(ManagerCookieEvents.Scheme),
            BusinessUpdateStatus.InvalidVersion => Failure(400, "invalid_version", "La versión del negocio no es válida."),
            _ => Failure(409, "business_version_conflict", "El negocio cambió. Actualiza los datos antes de guardar de nuevo.")
        };
    }

    private ObjectResult Failure(int status, string code, string title)
    {
        var problem = new ProblemDetails { Status = status, Title = title };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return new ObjectResult(problem) { StatusCode = status };
    }
}
