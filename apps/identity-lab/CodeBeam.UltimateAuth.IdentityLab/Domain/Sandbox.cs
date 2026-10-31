using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Domain;

public sealed class Sandbox
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public UserKey OwnerUserKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public SandboxStatus Status { get; set; } = SandboxStatus.Active;

    public SandboxSettings Settings { get; set; } = default!;

    public ICollection<SandboxMember> Members { get; set; } = new List<SandboxMember>();
}
