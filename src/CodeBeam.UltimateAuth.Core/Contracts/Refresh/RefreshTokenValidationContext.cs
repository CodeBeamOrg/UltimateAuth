using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record RefreshTokenValidationContext
    {
        public string? TenantId { get; init; }
        public string RefreshToken { get; init; } = default!;
        public DateTimeOffset Now { get; init; }

        // For Hybrid & Advanced
        public required DeviceContext Device { get; init; }
        public AuthSessionId? ExpectedSessionId { get; init; }
    }
}
