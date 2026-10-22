using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record SandboxSummary(
    Guid Id,
    string Name,
    UserKey OwnerUserKey,
    SandboxStatus Status,
    int MemberCount,
    int MaxUsers,
    DateTimeOffset CreatedAt);
