namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record SandboxStatistics(
    int Total,
    int Active,
    int Passive,
    int Suspended,
    int TotalMembers);
