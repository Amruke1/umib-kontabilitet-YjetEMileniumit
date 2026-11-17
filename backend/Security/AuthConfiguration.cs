using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Data;
using MiniERP.Models;

namespace MiniERP.Security;

public static class AuthConfiguration
{
    public static void AddApplicationSecurity(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<AuthSetupOptions>(configuration.GetSection("Auth"));
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        }).AddRoles<IdentityRole<int>>().AddEntityFrameworkStores<AuthDbContext>().AddSignInManager();
        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = environment.IsDevelopment() ? "mini-erp-auth" : "__Host-mini-erp-auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.CacheControl = "no-store";
                return context.Response.WriteAsJsonAsync(new ProblemDetails { Detail = "Hyni në llogari për të vazhduar." });
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.Headers.CacheControl = "no-store";
                return context.Response.WriteAsJsonAsync(new ProblemDetails { Detail = "Nuk keni leje për këtë veprim." });
            };
        });
        services.AddDataProtection().SetApplicationName("MiniERP")
            .PersistKeysToDbContext<AuthDbContext>();
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = environment.IsDevelopment() ? "mini-erp-csrf" : "__Host-mini-erp-csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Path = "/";
            // CookiePolicy enforces Secure in production, including when Render terminates TLS.
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy(AccessPolicies.Administration, policy => policy.RequireRole(AppRoles.Administrator));
            options.AddPolicy(AccessPolicies.CustomerRead, policy =>
                policy.RequireRole(AppRoles.Administrator, AppRoles.Salesperson, AppRoles.Accountant));
            options.AddPolicy(AccessPolicies.CustomerWrite, policy =>
                policy.RequireRole(AppRoles.Administrator, AppRoles.Salesperson));
        });
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Detail = "Shumë tentativa hyrjeje. Provoni përsëri pas një minute."
                }, cancellationToken);
            };
        });
    }
}
