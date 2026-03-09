using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed class LogoutDeviceAdminRequest
{
    public required UserKey UserKey { get; init; }
    public required SessionChainId ChainId { get; init; }
}
