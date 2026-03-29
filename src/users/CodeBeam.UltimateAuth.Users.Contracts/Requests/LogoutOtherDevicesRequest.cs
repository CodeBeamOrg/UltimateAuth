using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record LogoutOtherDevicesRequest
{
    public required SessionChainId CurrentChainId { get; init; }
}
