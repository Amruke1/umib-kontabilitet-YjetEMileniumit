using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniERP.Data;

namespace MiniERP.Security;

public static class IdentitySetup
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AuthDbContext>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(AccountLock.Sql);
        foreach (var name in AppRoles.All)
        {
            if (await roles.RoleExistsAsync(name)) continue;
            var result = await roles.CreateAsync(new IdentityRole<int>(name));
            if (!result.Succeeded) throw new InvalidOperationException("Could not initialize the application roles.");
        }
        await transaction.CommitAsync();
    }

    public static Dictionary<string, string[]> Errors(IdentityResult result) => result.Errors
        .GroupBy(error => error.Code.Contains("Password", StringComparison.Ordinal) ? "password" : "userName")
        .ToDictionary(group => group.Key, group => group.Select(error => error.Code switch
        {
            "DuplicateUserName" => "Ky emër përdoruesi ekziston.",
            "InvalidUserName" => "Emri i përdoruesit përmban karaktere të palejuara.",
            "PasswordTooShort" => "Fjalëkalimi duhet të ketë të paktën 12 karaktere.",
            "PasswordMismatch" => "Fjalëkalimi aktual nuk është i saktë.",
            _ => "Të dhënat e përdoruesit nuk u ruajtën. Kontrolloni fushat dhe provoni përsëri."
        }).ToArray());
}
