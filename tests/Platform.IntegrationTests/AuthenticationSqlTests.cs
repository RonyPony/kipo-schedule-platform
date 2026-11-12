using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Identity;
using Platform.Domain.Identity;
using Platform.Infrastructure.Identity;
using Platform.Infrastructure.Persistence;

namespace Platform.IntegrationTests;

public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KIPO_TEST_SQL_CONNECTION")))
            Skip = "Configura KIPO_TEST_SQL_CONNECTION para ejecutar las pruebas con SQL Server real.";
    }
}

public sealed class AuthenticationSqlFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public DbContextOptions<PlatformDbContext> Options { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var source = Environment.GetEnvironmentVariable("KIPO_TEST_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(source)) return;
        var connection = new SqlConnectionStringBuilder(source) { InitialCatalog = "KipoAuthTests_" + Guid.NewGuid().ToString("N") };
        Options = new DbContextOptionsBuilder<PlatformDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.UseCompatibilityLevel(140)).Options;
        await using var database = new PlatformDbContext(Options);
        await database.Database.MigrateAsync();
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development").ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connection.ConnectionString,
                    ["Security:AllowedOrigins:0"] = "https://localhost"
                })));
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        if (Options is null) return;
        await using var database = new PlatformDbContext(Options);
        var target = new SqlConnectionStringBuilder(database.Database.GetConnectionString()).InitialCatalog;
        if (!target.StartsWith("KipoAuthTests_", StringComparison.Ordinal)) throw new InvalidOperationException("Base de pruebas no válida.");
        await database.Database.EnsureDeletedAsync();
    }

    public HttpClient Client(bool cookies = true) => Factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = cookies,
        AllowAutoRedirect = false
    });

    public async Task<(ApplicationUser User, string Password)> CreateUser(UserState state = UserState.Active, bool admin = false)
    {
        using var scope = Factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = Guid.NewGuid().ToString("N") + "@example.test";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = "Prueba de autenticación",
            State = state,
            IsPlatformAdmin = admin
        };
        var password = "Aa1!" + Guid.NewGuid().ToString("N");
        var result = await manager.CreateAsync(user, password);
        Assert.True(result.Succeeded, string.Join(",", result.Errors.Select(error => error.Code)));
        return (user, password);
    }

    public static async Task<HttpResponseMessage> Post(HttpClient client, string path, object? body = null)
    {
        var token = await client.GetFromJsonAsync<CsrfToken>("/api/v1/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add(token!.HeaderName, token.Token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }
}

public sealed class AuthenticationSqlTests(AuthenticationSqlFixture fixture) : IClassFixture<AuthenticationSqlFixture>
{
    [SqlFact]
    public async Task LoginUsesSecureCookieAndCurrentUserDoesNotExposeSecrets()
    {
        var account = await fixture.CreateUser();
        using var client = fixture.Client();
        var login = await AuthenticationSqlFixture.Post(client, "/api/v1/auth/login", new LoginRequest(account.User.Email!, account.Password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = login.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Host-Kipo.Manager="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", cookie, StringComparison.OrdinalIgnoreCase);
        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(account.User.Id, (await response.Content.ReadFromJsonAsync<CurrentUser>())!.Id);
    }

    [SqlFact]
    public async Task LoginRequiresCsrfAndRejectsForeignOrigin()
    {
        var account = await fixture.CreateUser();
        using var client = fixture.Client();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(account.User.Email!, account.Password));
        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
        client.DefaultRequestHeaders.Add("Origin", "https://untrusted.example.test");
        login = await AuthenticationSqlFixture.Post(client, "/api/v1/auth/login", new LoginRequest(account.User.Email!, account.Password));
        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
    }

    [SqlFact]
    public async Task FailedPasswordsLockTheAccountWithoutEnumeratingIt()
    {
        var account = await fixture.CreateUser();
        using var client = fixture.Client();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await AuthenticationSqlFixture.Post(client, "/api/v1/auth/login", new LoginRequest(account.User.Email!, "WrongPassword1!"));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        var locked = await AuthenticationSqlFixture.Post(client, "/api/v1/auth/login", new LoginRequest(account.User.Email!, account.Password));
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        await using var database = new PlatformDbContext(fixture.Options);
        Assert.True((await database.Users.FindAsync(account.User.Id))!.LockoutEnd > DateTimeOffset.UtcNow);
    }

    [SqlFact]
    public async Task DisabledUserImmediatelyLosesAnExistingSession()
    {
        var account = await fixture.CreateUser();
        using var client = fixture.Client();
        Assert.Equal(HttpStatusCode.OK, (await AuthenticationSqlFixture.Post(client, "/api/v1/auth/login", new LoginRequest(account.User.Email!, account.Password))).StatusCode);
        await using var database = new PlatformDbContext(fixture.Options);
        await database.Users.Where(user => user.Id == account.User.Id).ExecuteUpdateAsync(setters => setters.SetProperty(user => user.State, UserState.Disabled));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [SqlFact]
    public async Task ChangingSecurityStampRevokesTheSession()
    {
        var account = await fixture.CreateUser();
        using var client = fixture.Client();
        Assert.Equal(HttpStatusCode.OK, (await AuthenticationSqlFixture.Post(client, "/api/v1/auth/login", new LoginRequest(account.User.Email!, account.Password))).StatusCode);
        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await users.UpdateSecurityStampAsync((await users.FindByIdAsync(account.User.Id.ToString()))!);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [SqlFact]
    public async Task LogoutRevokesStolenCookieAndLogoutRequiresCsrf()
    {
        var account = await fixture.CreateUser();
        using var client = fixture.Client();
        var login = await AuthenticationSqlFixture.Post(client, "/api/v1/auth/login", new LoginRequest(account.User.Email!, account.Password));
        var cookie = login.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Host-Kipo.Manager=")).Split(';')[0];
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await AuthenticationSqlFixture.Post(client, "/api/v1/auth/logout")).StatusCode);
        using var replay = fixture.Client(false);
        replay.DefaultRequestHeaders.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [SqlFact]
    public async Task LogoutAllRevokesOtherSessions()
    {
        var account = await fixture.CreateUser();
        using var first = fixture.Client();
        using var second = fixture.Client();
        var request = new LoginRequest(account.User.Email!, account.Password);
        Assert.Equal(HttpStatusCode.OK, (await AuthenticationSqlFixture.Post(first, "/api/v1/auth/login", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AuthenticationSqlFixture.Post(second, "/api/v1/auth/login", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await AuthenticationSqlFixture.Post(first, "/api/v1/auth/logout-all")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [SqlFact]
    public async Task AbsoluteExpiryIsCheckedOnTheServer()
    {
        var account = await fixture.CreateUser();
        using var client = fixture.Client();
        Assert.Equal(HttpStatusCode.OK, (await AuthenticationSqlFixture.Post(client, "/api/v1/auth/login", new LoginRequest(account.User.Email!, account.Password))).StatusCode);
        await using var database = new PlatformDbContext(fixture.Options);
        await database.UserSessions.Where(session => session.UserId == account.User.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [SqlFact]
    public async Task PendingAccountCannotGainRolesAndAdminCannotUseManagerLogin()
    {
        var pending = await fixture.CreateUser(UserState.PendingApproval);
        using var client = fixture.Client();
        var response = await AuthenticationSqlFixture.Post(client, "/api/v1/auth/login", new LoginRequest(pending.User.Email!, pending.Password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("PendingApproval", (await response.Content.ReadFromJsonAsync<CurrentUser>())!.State);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/auth/me")).StatusCode);
        var admin = await fixture.CreateUser(admin: true);
        using var administrativeClient = fixture.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthenticationSqlFixture.Post(administrativeClient, "/api/v1/auth/login", new LoginRequest(admin.User.Email!, admin.Password))).StatusCode);
    }

    [SqlFact]
    public async Task DatabaseEnforcesUniqueNormalizedEmail()
    {
        var original = await fixture.CreateUser();
        await using var database = new PlatformDbContext(fixture.Options);
        database.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            DisplayName = "Duplicado",
            UserName = Guid.NewGuid().ToString(),
            NormalizedUserName = Guid.NewGuid().ToString(),
            Email = original.User.Email!.ToUpperInvariant(),
            NormalizedEmail = original.User.NormalizedEmail
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }
}
