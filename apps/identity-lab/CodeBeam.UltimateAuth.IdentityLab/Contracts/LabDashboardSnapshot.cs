namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record LabDashboardSnapshot(
    Guid SandboxId,
    string SandboxName,
    int MaxUsers,
    int MemberCount,
    int RemainingCapacity,
    bool IsOwner,

    string? DisplayName = null,
    string? Username = null,
    string? Email = null,
    string? AccountStatus = null,

    int? ChainCount = null,
    int? RoleCount = null);
