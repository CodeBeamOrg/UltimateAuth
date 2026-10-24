using CodeBeam.UltimateAuth.IdentityLab.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public enum SandboxAccessFailure
{
    None = 0,
    InvalidUser,
    NotMember,
    NotFound,
    Passive,
    Suspended,
    OwnerRequired
}

public sealed record SandboxAccessResult(bool Allowed, SandboxAccessFailure Failure, Guid? SandboxId, SandboxMemberRole? Role, SandboxStatus? Status)
{
    public static SandboxAccessResult Success(
        Guid sandboxId,
        SandboxMemberRole role,
        SandboxStatus status)
        => new(true, SandboxAccessFailure.None,
            sandboxId, role, status);

    public static SandboxAccessResult Denied(
        SandboxAccessFailure failure,
        Guid? sandboxId = null,
        SandboxMemberRole? role = null,
        SandboxStatus? status = null)
        => new(false, failure, sandboxId, role, status);
}
