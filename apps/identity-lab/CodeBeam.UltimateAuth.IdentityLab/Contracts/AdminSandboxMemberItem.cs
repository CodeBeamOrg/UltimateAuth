using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record AdminSandboxMemberItem(
    UserKey UserKey,
    string DisplayName,
    string Email,
    SandboxMemberRole Role,
    UserStatus Status,
    DateTimeOffset JoinedAt);
