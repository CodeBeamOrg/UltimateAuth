using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record LogoutOtherDevicesSelfRequest
{
    public required SessionChainId CurrentChainId { get; init; }
}
