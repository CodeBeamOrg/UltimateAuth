using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Abstractions;

public sealed record UserRuntimeRecord
{
    public UserKey UserKey { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
}
