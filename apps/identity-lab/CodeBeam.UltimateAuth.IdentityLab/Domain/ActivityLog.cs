namespace CodeBeam.UltimateAuth.IdentityLab.Domain;

public sealed class ActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SandboxId { get; set; }

    public string ActorUserKey { get; set; } = default!;

    public string EventType { get; set; } = default!;

    public string? TargetUserKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Sandbox Sandbox { get; set; } = default!;
}
