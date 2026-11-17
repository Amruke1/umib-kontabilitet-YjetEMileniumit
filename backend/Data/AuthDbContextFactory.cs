using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MiniERP.Data;

public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<AuthDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=mini_erp;Username=postgres";
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(connection, database => database.MigrationsHistoryTable("__EFMigrationsHistory", "auth"))
            .Options;
        return new AuthDbContext(options);
    }
}
