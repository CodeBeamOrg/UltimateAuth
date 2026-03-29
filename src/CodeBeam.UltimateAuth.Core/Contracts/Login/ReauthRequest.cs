using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record ReauthRequest
{
    public AuthSessionId SessionId { get; init; }
    public required string Secret { get; init; }
}
