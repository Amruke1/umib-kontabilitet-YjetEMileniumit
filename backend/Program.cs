using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Services;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins("http://127.0.0.1:5173", "http://localhost:5173")
        .WithMethods("GET", "POST", "PUT", "DELETE").AllowAnyHeader()));

var app = builder.Build();

app.UseCors("Frontend");
app.MapControllers();

app.Run();
