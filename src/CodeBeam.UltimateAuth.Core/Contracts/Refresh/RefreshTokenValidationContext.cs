using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record RefreshTokenValidationContext<TUserId>
    {
        public string? TenantId { get; init; }
        public string RefreshToken { get; init; } = default!;
        public DateTimeOffset Now { get; init; }

        // For Hybrid & Advanced
        public DeviceInfo? Device { get; init; }
        public AuthSessionId? ExpectedSessionId { get; init; }
    }
}
