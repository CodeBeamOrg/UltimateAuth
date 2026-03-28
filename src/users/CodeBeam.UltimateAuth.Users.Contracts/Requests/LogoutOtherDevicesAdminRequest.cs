using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record LogoutOtherDevicesAdminRequest
{
    public required SessionChainId CurrentChainId { get; init; }
}
