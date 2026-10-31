namespace CodeBeam.UltimateAuth.IdentityLab.Authorization;

public sealed class LabAdminOptions
{
    public const string SectionName = "LabAdmin";

    public string Username { get; set; } = "Ladmin";
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "Lab Administrator";
    public string Password { get; set; } = string.Empty;
}
