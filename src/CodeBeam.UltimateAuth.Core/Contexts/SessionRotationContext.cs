using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contexts
{
    public sealed record SessionRotationContext<TUserId>
    {
        public string? TenantId { get; init; }
        public AuthSessionId CurrentSessionId { get; init; }
        public TUserId UserId { get; init; }
        public DateTime Now { get; init; }
        public DeviceInfo Device { get; init; }
        public ClaimsSnapshot Claims { get; init; }
        public SessionMetadata Metadata { get; init; }
    }
}
