namespace CodeBeam.UltimateAuth.IdentityLab.Authorization;

public static class LabRoles
{
    public const string Admin = "Ladmin";
    public const string Owner = "Owner";
    public const string User = "User";

    public static readonly IReadOnlyList<string> SystemRoles =
    [
        Admin,
        Owner,
        User
    ];
}
