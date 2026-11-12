using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Platform.Application.Observability;
using Platform.Infrastructure.Observability;
using Platform.Api.Identity;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args);
}
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["code"] = context.ProblemDetails.Status switch
        {
            404 => "resource_not_found",
            _ => "request_failed"
        };
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Title = context.ProblemDetails.Status switch
        {
            404 => "No se encontró el recurso solicitado.",
            _ => "No se pudo completar la solicitud."
        };
        context.ProblemDetails.Detail = null;
    };
});
builder.Services.AddOpenApi("v1");
builder.AddPlatformAuthentication();
builder.Services.AddHealthChecks().AddCheck<SqlServerHealthCheck>("sql-server", tags: ["ready"]);

var app = builder.Build();
if (builder.Configuration.GetValue<bool>("Admin:Create") || builder.Configuration.GetValue<bool>("Admin:ResetMfa"))
{
    await AdminBootstrap.RunAsync(app.Services, builder.Configuration.GetValue<bool>("Admin:ResetMfa"));
    return;
}
if (builder.Configuration.GetValue<bool>("LocalUser:Create"))
{
    await LocalUserBootstrap.RunAsync(app.Services, app.Environment);
    return;
}
if (app.Environment.IsDevelopment()) app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    var supplied = context.Request.Headers["X-Correlation-ID"].ToString();
    var correlationId = Guid.TryParseExact(supplied, "D", out var parsed)
        ? parsed.ToString("D")
        : Guid.NewGuid().ToString("D");
    context.TraceIdentifier = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers.CacheControl = "no-store";
    using var scope = app.Logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });
    var started = Stopwatch.GetTimestamp();
    try
    {
        await next(context);
    }
    finally
    {
        app.Logger.LogInformation("HTTP {Method} {StatusCode} {ElapsedMs}ms",
            context.Request.Method, context.Response.StatusCode, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
});
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/api/v1/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/api/v1/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapGet("/api/v1/status", () => new ServiceStatus("Kipo", "available", "v1"))
    .WithName("GetServiceStatus");
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Kipo API v1");
        options.DocumentTitle = "Kipo · API";
        options.UseRequestInterceptor("(request) => { if (['POST','PUT','PATCH','DELETE'].includes((request.method || 'GET').toUpperCase())) { return fetch(new URL(request.url, window.location.origin).pathname.startsWith('/api/v1/admin/') ? '/api/v1/admin/auth/csrf' : '/api/v1/auth/csrf', { credentials: 'same-origin' }).then(response => { if (!response.ok) throw new Error('No se pudo obtener el token de seguridad.'); return response.json(); }).then(token => { request.headers[token.headerName] = token.token; return request; }); } return request; }");
    });
}
app.Run();

public partial class Program;
