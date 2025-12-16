using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record SessionAccessContext
    {
        public SessionState State { get; init; }

        public bool IsExpired { get; init; }

        public bool IsRevoked { get; init; }

        public string? ChainId { get; init; }

        public string? BoundDeviceId { get; init; }
    }

}
