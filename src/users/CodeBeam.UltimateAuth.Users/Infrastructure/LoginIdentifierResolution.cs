using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users;
public sealed record LoginIdentifierResolution
{
    public required TenantKey Tenant { get; init; }
    public UserKey? UserKey { get; init; }
    public required string RawIdentifier { get; init; }
    public required string NormalizedIdentifier { get; init; }

    public UserIdentifierType? BuiltInType { get; init; }
    public string? CustomType { get; init; }

    public bool IsVerified { get; init; }
}
