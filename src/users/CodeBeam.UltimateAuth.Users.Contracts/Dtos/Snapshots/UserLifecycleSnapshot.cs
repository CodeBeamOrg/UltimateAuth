using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserLifecycleSnapshot
{
    public UserKey UserKey { get; init; }
    public UserStatus Status { get; init; }
}
