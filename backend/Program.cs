var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<MiniERP.Services.CustomerService>();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins("http://127.0.0.1:5173", "http://localhost:5173")
        .WithMethods("GET", "POST").AllowAnyHeader()));

var app = builder.Build();

app.UseCors("Frontend");
app.MapControllers();

app.Run();
