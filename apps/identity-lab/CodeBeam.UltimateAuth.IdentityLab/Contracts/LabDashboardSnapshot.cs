namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record LabDashboardSnapshot(
    Guid SandboxId,
    string SandboxName,
    int MaxUsers,
    int MemberCount,
    int RemainingCapacity,
    bool IsOwner);
