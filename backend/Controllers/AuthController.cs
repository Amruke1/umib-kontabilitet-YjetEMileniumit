using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniERP.Data;
using MiniERP.Models;
using MiniERP.Security;

namespace MiniERP.Controllers;

[ApiController]
[Route("api/auth")]
[RequestSizeLimit(8192)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    AuthDbContext db,
    IAntiforgery antiforgery,
    IOptions<AuthSetupOptions> setup) : ControllerBase
{
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult Csrf() => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpGet("setup")]
    [AllowAnonymous]
    public async Task<IActionResult> SetupStatus() => Ok(new
    {
        required = !await db.Users.AnyAsync(),
        available = setup.Value.BootstrapToken.Length >= 32
    });

    [HttpPost("setup")]
    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> Setup(BootstrapRequest input)
    {
        var expected = setup.Value.BootstrapToken;
        if (expected.Length < 32 || !CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
            SHA256.HashData(Encoding.UTF8.GetBytes(input.SetupToken))))
            return Unauthorized(new ProblemDetails { Detail = "Kodi i konfigurimit nuk është i saktë." });

        ApplicationUser user;
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlRawAsync(AccountLock.Sql);
            if (await db.Users.AnyAsync())
                return Conflict(new ProblemDetails { Detail = "Administratori fillestar është krijuar tashmë." });
            user = new ApplicationUser { UserName = input.UserName.Trim(), DisplayName = input.DisplayName.Trim() };
            var created = await users.CreateAsync(user, input.Password);
            if (!created.Succeeded) return ValidationProblem(new ValidationProblemDetails(IdentitySetup.Errors(created)));
            var assigned = await users.AddToRoleAsync(user, AppRoles.Administrator);
            if (!assigned.Succeeded) return ValidationProblem(new ValidationProblemDetails(IdentitySetup.Errors(assigned)));
            await transaction.CommitAsync();
        }
        await signIn.SignInAsync(user, isPersistent: false);
        return Ok(await Summary(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> Login(LoginRequest input)
    {
        var user = await users.FindByNameAsync(input.UserName.Trim());
        if (user is null || !user.IsActive)
            return Unauthorized(new ProblemDetails { Detail = "Emri i përdoruesit ose fjalëkalimi nuk është i saktë." });
        var result = await signIn.PasswordSignInAsync(user, input.Password, isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized(new ProblemDetails { Detail = "Emri i përdoruesit ose fjalëkalimi nuk është i saktë. Pas shumë tentativave, provoni më vonë." });
        return Ok(await Summary(user));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await users.GetUserAsync(User);
        return user is { IsActive: true } ? Ok(await Summary(user)) : Unauthorized();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var user = await users.GetUserAsync(User);
        if (user is not null)
        {
            var revoked = await users.UpdateSecurityStampAsync(user);
            if (!revoked.Succeeded) return Problem("Dalja nuk u përfundua. Provoni përsëri.");
        }
        await signIn.SignOutAsync();
        return NoContent();
    }

    [HttpPost("password")]
    [Authorize]
    public async Task<IActionResult> Password(ChangePasswordRequest input)
    {
        var user = await users.GetUserAsync(User);
        if (user is not { IsActive: true }) return Unauthorized();
        var result = await users.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
        if (!result.Succeeded) return ValidationProblem(new ValidationProblemDetails(IdentitySetup.Errors(result)));
        await signIn.RefreshSignInAsync(user);
        return NoContent();
    }

    private async Task<UserSummary> Summary(ApplicationUser user) => new(
        user.Id, user.UserName!, user.DisplayName, (await users.GetRolesAsync(user)).Single(), user.IsActive);
}

public sealed record UserSummary(int Id, string UserName, string DisplayName, string Role, bool IsActive);

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Shkruani emrin e përdoruesit."), MaxLength(100)]
    public string UserName { get; init; } = string.Empty;
    [Required(ErrorMessage = "Shkruani fjalëkalimin."), MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}

public class NewAccountRequest
{
    [Required(ErrorMessage = "Shkruani emrin e përdoruesit."), StringLength(100, MinimumLength = 3)]
    public string UserName { get; init; } = string.Empty;
    [Required(ErrorMessage = "Shkruani emrin e plotë."), MaxLength(100)]
    public string DisplayName { get; init; } = string.Empty;
    [Required(ErrorMessage = "Shkruani fjalëkalimin."), StringLength(128, MinimumLength = 12,
        ErrorMessage = "Fjalëkalimi duhet të ketë nga 12 deri në 128 karaktere.")]
    public string Password { get; init; } = string.Empty;
}

public sealed class BootstrapRequest : NewAccountRequest
{
    [Required(ErrorMessage = "Shkruani kodin privat të konfigurimit."), MaxLength(256)]
    public string SetupToken { get; init; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    [Required, MaxLength(128)]
    public string CurrentPassword { get; init; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 12, ErrorMessage = "Fjalëkalimi duhet të ketë nga 12 deri në 128 karaktere.")]
    public string NewPassword { get; init; } = string.Empty;
}
