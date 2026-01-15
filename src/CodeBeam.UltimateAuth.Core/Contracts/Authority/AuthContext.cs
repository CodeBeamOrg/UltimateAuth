using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record AuthContext
    {
        public string? TenantId { get; init; }

        public AuthOperation Operation { get; init; }

        public UAuthMode Mode { get; init; }

        public SessionSecurityContext? Session { get; init; }

        public required DeviceContext Device { get; init; }

        public DateTimeOffset At { get; init; }
    }
}
