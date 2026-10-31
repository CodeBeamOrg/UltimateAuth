using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Domain;

public sealed class SandboxMember
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SandboxId { get; set; }

    public UserKey UserKey { get; set; }

    public SandboxMemberRole Role { get; set; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public Sandbox Sandbox { get; set; } = default!;
}

public enum SandboxMemberRole
{
    Member = 0,
    Owner = 1
}
