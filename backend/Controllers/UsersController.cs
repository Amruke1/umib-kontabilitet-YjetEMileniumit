using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Models;
using MiniERP.Security;

namespace MiniERP.Controllers;

[ApiController]
[Route("api/users")]
[RequestSizeLimit(8192)]
[Authorize(Policy = AccessPolicies.Administration)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UsersController(UserManager<ApplicationUser> users, AuthDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var accounts = await (from user in db.Users.AsNoTracking()
                              join membership in db.UserRoles on user.Id equals membership.UserId
                              join role in db.Roles on membership.RoleId equals role.Id
                              orderby user.UserName
                              select new UserSummary(user.Id, user.UserName!, user.DisplayName,
                                  role.Name!, user.IsActive)).ToListAsync();
        return Ok(accounts);
    }

    [HttpPost]
    public async Task<IActionResult> Post(CreateUserRequest input)
    {
        if (!AppRoles.All.Contains(input.Role))
            return BadRequest(new ProblemDetails { Detail = "Zgjidhni një nga katër rolet e sistemit." });
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(AccountLock.Sql);
        if (!await IsCurrentAdministrator()) return Forbid();
        var user = new ApplicationUser { UserName = input.UserName.Trim(), DisplayName = input.DisplayName.Trim() };
        var created = await users.CreateAsync(user, input.Password);
        if (!created.Succeeded) return ValidationProblem(new ValidationProblemDetails(IdentitySetup.Errors(created)));
        var assigned = await users.AddToRoleAsync(user, input.Role);
        if (!assigned.Succeeded) return ValidationProblem(new ValidationProblemDetails(IdentitySetup.Errors(assigned)));
        await transaction.CommitAsync();
        return Created($"/api/users/{user.Id}", new UserSummary(user.Id, user.UserName!, user.DisplayName, input.Role, true));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, UpdateUserRequest input)
    {
        if (!AppRoles.All.Contains(input.Role))
            return BadRequest(new ProblemDetails { Detail = "Zgjidhni një nga katër rolet e sistemit." });
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(AccountLock.Sql);
        if (!await IsCurrentAdministrator()) return Forbid();
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        if (users.GetUserId(User) == id.ToString())
            return BadRequest(new ProblemDetails { Detail = "Nuk mund ta ndryshoni rolin ose statusin e llogarisë suaj këtu." });
        user.DisplayName = input.DisplayName.Trim();
        user.IsActive = input.IsActive;
        var updated = await users.UpdateAsync(user);
        if (!updated.Succeeded) return ValidationProblem(new ValidationProblemDetails(IdentitySetup.Errors(updated)));
        var previousRoles = await users.GetRolesAsync(user);
        if (previousRoles.Count != 1 || previousRoles[0] != input.Role)
        {
            var removed = await users.RemoveFromRolesAsync(user, previousRoles);
            if (!removed.Succeeded) return ValidationProblem(new ValidationProblemDetails(IdentitySetup.Errors(removed)));
            var assigned = await users.AddToRoleAsync(user, input.Role);
            if (!assigned.Succeeded) return ValidationProblem(new ValidationProblemDetails(IdentitySetup.Errors(assigned)));
        }
        var revoked = await users.UpdateSecurityStampAsync(user);
        if (!revoked.Succeeded) return ValidationProblem(new ValidationProblemDetails(IdentitySetup.Errors(revoked)));
        await transaction.CommitAsync();
        return Ok(new UserSummary(user.Id, user.UserName!, user.DisplayName, input.Role, user.IsActive));
    }

    private async Task<bool> IsCurrentAdministrator()
    {
        if (!int.TryParse(users.GetUserId(User), out var currentId)) return false;
        var current = await db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == currentId);
        return current is { IsActive: true } && await users.IsInRoleAsync(current, AppRoles.Administrator);
    }
}

public sealed class CreateUserRequest : NewAccountRequest
{
    [Required]
    public string Role { get; init; } = string.Empty;
}

public sealed class UpdateUserRequest
{
    [Required(ErrorMessage = "Shkruani emrin e plotë."), MaxLength(100)]
    public string DisplayName { get; init; } = string.Empty;
    [Required]
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
