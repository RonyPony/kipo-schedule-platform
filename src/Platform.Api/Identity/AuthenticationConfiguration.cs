using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Identity;
using Platform.Infrastructure.Identity;
using Platform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Platform.Application.Businesses;
using Platform.Infrastructure.Businesses;

namespace Platform.Api.Identity;

public static class AuthenticationConfiguration
{
    public static void AddPlatformAuthentication(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<PlatformDbContext>(options => options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"), sql => sql.UseCompatibilityLevel(140)));
        services.AddScoped<BusinessContext>();
        services.AddDbContext<BusinessDbContext>((provider, options) => options.UseSqlServer(
            provider.GetRequiredService<PlatformDbContext>().Database.GetDbConnection(), sql => sql.UseCompatibilityLevel(140)));
        services.AddScoped<IBusinessAccessService, BusinessAccessService>();
        services.AddScoped<IBusinessService, BusinessService>();
        services.AddScoped<IAuthorizationHandler, BusinessAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, BusinessAuthorizationResultHandler>();
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<PlatformDbContext>().AddDefaultTokenProviders();
        services.AddScoped<IUserStore<ApplicationUser>, ProtectedUserStore>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAdminAuthenticationService, AdminAuthenticationService>();
        services.AddScoped<AdminCookieEvents>();
        services.AddScoped<ManagerCookieEvents>();
        services.AddScoped<CsrfValidationFilter>();
        services.AddAuthentication("Kipo").AddPolicyScheme("Kipo", null, options =>
        {
            options.ForwardDefaultSelector = context => context.Request.Path.StartsWithSegments("/api/v1/admin")
                ? AdminCookieEvents.Scheme : ManagerCookieEvents.Scheme;
        }).AddCookie(AdminCookieEvents.Scheme, options =>
        {
            options.Cookie.Name = "__Host-Kipo.Admin";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;
            options.EventsType = typeof(AdminCookieEvents);
        }).AddCookie(ManagerCookieEvents.Scheme, options =>
        {
            options.Cookie.Name = "__Host-Kipo.Manager";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
            options.SlidingExpiration = true;
            options.EventsType = typeof(ManagerCookieEvents);
        });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminCookieEvents.Policy, policy => policy.AddAuthenticationSchemes(AdminCookieEvents.Scheme)
                .RequireAuthenticatedUser().RequireClaim("amr", "mfa"));
            options.AddPolicy(BusinessAuthorizationHandler.MemberPolicy, policy => policy
                .AddAuthenticationSchemes(ManagerCookieEvents.Scheme).RequireAuthenticatedUser().AddRequirements(new BusinessRequirement(false)));
            options.AddPolicy(BusinessAuthorizationHandler.OwnerPolicy, policy => policy
                .AddAuthenticationSchemes(ManagerCookieEvents.Scheme).RequireAuthenticatedUser().AddRequirements(new BusinessRequirement(true)));
        });
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-Kipo.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        services.AddControllers().ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problem = new ValidationProblemDetails(context.ModelState)
                {
                    Status = 400,
                    Title = "Revisa los datos introducidos."
                };
                problem.Extensions["code"] = "validation_failed";
                problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                return new BadRequestObjectResult(problem);
            };
        });
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });
        var keys = builder.Configuration["DataProtection:KeyPath"] ?? Path.Combine(builder.Environment.ContentRootPath, ".local", "keys");
        var protection = services.AddDataProtection().SetApplicationName("Kipo").PersistKeysToFileSystem(new DirectoryInfo(keys));
        if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
        if (builder.Environment.IsDevelopment())
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 1;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
                options.KnownProxies.Add(IPAddress.Loopback);
                options.KnownProxies.Add(IPAddress.IPv6Loopback);
            });
        }
    }
}
