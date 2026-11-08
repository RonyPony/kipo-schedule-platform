using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Platform.Application.Observability;

namespace Platform.IntegrationTests;

public sealed class HealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public HealthTests(WebApplicationFactory<Program> factory)
    {
        client = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ""
            }))).CreateClient();
    }

    [Fact]
    public async Task LivenessDoesNotDependOnDatabase()
    {
        var response = await client.GetAsync("/api/v1/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadinessFailsWithoutDatabaseAndDoesNotExposeConfiguration()
    {
        var response = await client.GetAsync("/api/v1/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("untrusted-value")]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    public async Task ErrorsHaveSanitizedCorrelationAndProblemDetails(string correlation)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/does-not-exist");
        request.Headers.Add("X-Correlation-ID", correlation);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var id = response.Headers.GetValues("X-Correlation-ID").Single();
        Assert.True(Guid.TryParseExact(id, "D", out _));
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, problem.GetProperty("traceId").GetString());
        Assert.Equal("resource_not_found", problem.GetProperty("code").GetString());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task StatusIdentifiesKipoWithoutInventingDatabaseReadiness()
    {
        var status = await client.GetFromJsonAsync<ServiceStatus>("/api/v1/status");
        Assert.Equal("Kipo", status?.Product);
        Assert.Equal("v1", status?.ApiVersion);
    }

    [Fact]
    public async Task SwaggerIsAvailableOnlyInDevelopment()
    {
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
        using var production = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var productionClient = production.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await productionClient.GetAsync("/swagger/index.html")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await productionClient.GetAsync("/openapi/v1.json")).StatusCode);
    }
}
