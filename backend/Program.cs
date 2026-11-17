using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Services;
using MiniERP.Security;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var configuredPort = builder.Configuration["PORT"];
if (!string.IsNullOrWhiteSpace(configuredPort))
{
    if (!int.TryParse(configuredPort, out var port) || port is < 1 or > 65535)
    {
        throw new InvalidOperationException("PORT must be an integer between 1 and 65535.");
    }

    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

static string DatabaseConnection(IServiceProvider services)
{
    var connection = services.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
    return !string.IsNullOrWhiteSpace(connection) ? connection : throw new InvalidOperationException(
        "Configure ConnectionStrings:DefaultConnection using .NET User Secrets " +
        "or ConnectionStrings__DefaultConnection. See docs/database-setup.md.");
}

builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddDbContext<AppDbContext>((services, options) => options.UseNpgsql(DatabaseConnection(services)));
builder.Services.AddDbContext<AuthDbContext>((services, options) => options.UseNpgsql(DatabaseConnection(services),
    database => database.MigrationsHistoryTable("__EFMigrationsHistory", "auth")));
builder.Services.AddApplicationSecurity(builder.Configuration, builder.Environment);
builder.Services.AddScoped<CustomerService>();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? (builder.Environment.IsDevelopment()
        ? ["http://127.0.0.1:5173", "http://localhost:5173"]
        : []);
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins)
        .WithMethods("GET", "POST", "PUT", "DELETE").AllowAnyHeader().AllowCredentials()));

var app = builder.Build();

{
    await using var scope = app.Services.CreateAsyncScope();
    if (app.Configuration.GetValue<bool>("Database:InitializeSchema"))
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Database", "customers.sql");
        await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(schemaPath));
        await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();
    }
    await IdentitySetup.SeedRolesAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
    app.UseCookiePolicy(new CookiePolicyOptions { Secure = CookieSecurePolicy.Always });

var servesFrontend = File.Exists(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "index.html"));
if (servesFrontend)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

if (servesFrontend)
{
    app.MapFallback("/api/{**path}", () => Results.NotFound()).AllowAnonymous();
    app.MapFallback("/health/{**path}", () => Results.NotFound()).AllowAnonymous();
    app.MapFallbackToFile("index.html").AllowAnonymous();
}

app.Run();

public partial class Program;
