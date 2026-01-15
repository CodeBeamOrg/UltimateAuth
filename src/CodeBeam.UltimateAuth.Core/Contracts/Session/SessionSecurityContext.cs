using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record SessionSecurityContext
    {
        public required UserKey? UserKey { get; init; }

        public required AuthSessionId SessionId { get; init; }

        public SessionState State { get; init; }

        public SessionChainId? ChainId { get; init; }

        public DeviceId? BoundDeviceId { get; init; }
    }

}
