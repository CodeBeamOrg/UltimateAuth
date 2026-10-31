using CodeBeam.UltimateAuth.IdentityLab.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record AdminDashboardSnapshot(
    int TotalSandboxes,
    int ActiveSandboxes,
    int SuspendedSandboxes,
    int TotalMembers);
