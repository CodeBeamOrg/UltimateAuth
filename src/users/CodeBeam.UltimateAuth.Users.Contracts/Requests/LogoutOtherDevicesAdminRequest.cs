using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed class LogoutOtherDevicesAdminRequest
{
    public required UserKey UserKey { get; init; }
    public required SessionChainId CurrentChainId { get; init; }
}
