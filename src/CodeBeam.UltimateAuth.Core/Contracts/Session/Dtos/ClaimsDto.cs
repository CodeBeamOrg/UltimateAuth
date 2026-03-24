namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class ClaimsDto
{
    public Dictionary<string, string[]> Claims { get; set; } = new();

    public string[] Roles { get; set; } = Array.Empty<string>();

    public string[] Permissions { get; set; } = Array.Empty<string>();
}
