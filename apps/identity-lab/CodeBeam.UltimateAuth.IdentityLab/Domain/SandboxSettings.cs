namespace CodeBeam.UltimateAuth.IdentityLab.Domain;

public sealed class SandboxSettings
{
    public Guid SandboxId { get; set; }

    public int MaxUsers { get; set; } = 5;

    public Sandbox Sandbox { get; set; } = default!;
}
