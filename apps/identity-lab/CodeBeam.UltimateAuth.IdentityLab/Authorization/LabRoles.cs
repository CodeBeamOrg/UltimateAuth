public static class LabRoles
{
    public const string Admin = "Ladmin";
    public const string Owner = "Owner";
    public const string User = "User";

    public const string Manager = "Manager";
    public const string Customer = "Customer";

    public static readonly IReadOnlyList<string> SystemRoles =
    [
        Admin,
        Owner,
        User
    ];

    public static readonly IReadOnlyList<string> DemoRoles =
    [
        Manager,
        Customer
    ];

    public static bool IsDemoRole(string? name) => DemoRoles.Contains(name ?? "", StringComparer.OrdinalIgnoreCase);
}
