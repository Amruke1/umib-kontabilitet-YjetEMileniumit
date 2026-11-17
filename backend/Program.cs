using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Services;

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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure ConnectionStrings:DefaultConnection using .NET User Secrets " +
        "or ConnectionStrings__DefaultConnection. See docs/database-setup.md.");
}

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<CustomerService>();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? (builder.Environment.IsDevelopment()
        ? ["http://127.0.0.1:5173", "http://localhost:5173"]
        : []);
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins)
        .WithMethods("GET", "POST", "PUT", "DELETE").AllowAnyHeader()));

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:InitializeSchema"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var schemaPath = Path.Combine(AppContext.BaseDirectory, "Database", "customers.sql");
    await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(schemaPath));
}

var servesFrontend = File.Exists(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "index.html"));
if (servesFrontend)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseCors("Frontend");
app.MapControllers();

if (servesFrontend)
{
    app.MapFallback("/api/{**path}", () => Results.NotFound());
    app.MapFallback("/health/{**path}", () => Results.NotFound());
    app.MapFallbackToFile("index.html");
}

app.Run();
