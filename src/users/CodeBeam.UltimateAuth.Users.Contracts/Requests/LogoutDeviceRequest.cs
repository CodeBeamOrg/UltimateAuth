using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record LogoutDeviceRequest
{
    public required SessionChainId ChainId { get; init; }
}
