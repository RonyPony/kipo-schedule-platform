using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Platform.Api.Identity;

public sealed class CsrfValidationFilter(IAntiforgery antiforgery, IConfiguration configuration) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method)) return;
        var origin = request.Headers.Origin.ToString();
        var allowed = configuration.GetSection("Security:AllowedOrigins").Get<string[]>() ?? [];
        if (!string.IsNullOrEmpty(origin) && !allowed.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            Reject(context);
            return;
        }
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            Reject(context);
        }
    }

    private static void Reject(AuthorizationFilterContext context)
    {
        var problem = new ProblemDetails { Status = 400, Title = "La solicitud de seguridad no es válida. Actualiza la página e inténtalo de nuevo." };
        problem.Extensions["code"] = "csrf_validation_failed";
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.Result = new ObjectResult(problem) { StatusCode = 400 };
    }
}
