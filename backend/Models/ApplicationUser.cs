using Microsoft.AspNetCore.Identity;

namespace MiniERP.Models;

public sealed class ApplicationUser : IdentityUser<int>
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
