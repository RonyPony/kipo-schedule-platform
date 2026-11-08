using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Platform.Application.Observability;
using Platform.Infrastructure.Observability;

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
builder.Services.AddHealthChecks().AddCheck<SqlServerHealthCheck>("sql-server", tags: ["ready"]);

var app = builder.Build();
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
app.MapHealthChecks("/api/v1/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/api/v1/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapGet("/api/v1/status", () => new ServiceStatus("Kipo", "available", "v1"))
    .WithName("GetServiceStatus");
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.Run();

public partial class Program;
