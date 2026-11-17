namespace MiniERP.Security;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Salesperson = "Salesperson";
    public const string Warehouse = "Warehouse";
    public const string Accountant = "Accountant";
    public static readonly string[] All = [Administrator, Salesperson, Warehouse, Accountant];
}

public static class AccessPolicies
{
    public const string CustomerRead = "CustomerRead";
    public const string CustomerWrite = "CustomerWrite";
    public const string Administration = "Administration";
}

public sealed class AuthSetupOptions
{
    public string BootstrapToken { get; set; } = string.Empty;
}

public static class AccountLock
{
    // Serialize initial setup and administrator changes, including requests on different instances.
    public const string Sql = "SELECT pg_advisory_xact_lock(731930674141)";
}
