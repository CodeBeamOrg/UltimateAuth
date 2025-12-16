using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record SessionValidationContext
    {
        public string? TenantId { get; init; }
        public AuthSessionId SessionId { get; init; }
        public DateTime Now { get; init; }
        public DeviceInfo Device { get; init; }
    }
}
