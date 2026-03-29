using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record ExternalLoginRequest
{
    public required string Provider { get; init; }
    public required string ExternalToken { get; init; }
    public required DeviceContext Device { get; init; }
}
