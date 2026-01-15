using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record SessionRotationContext
    {
        public string? TenantId { get; init; }
        public AuthSessionId CurrentSessionId { get; init; }
        public UserKey UserKey { get; init; }
        public DateTimeOffset Now { get; init; }
        public required DeviceContext Device { get; init; }
        public ClaimsSnapshot? Claims { get; init; }
        public required SessionMetadata Metadata { get; init; } = SessionMetadata.Empty;
    }
}
