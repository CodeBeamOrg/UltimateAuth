using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record AdminSandboxItem(
    Guid SandboxId,
    string Name,
    UserKey OwnerUserKey,
    string OwnerName,
    string OwnerEmail,
    SandboxStatus Status,
    int MemberCount,
    int MaxUsers)
{
    public int RemainingCapacity => Math.Max(0, MaxUsers - MemberCount);
}
