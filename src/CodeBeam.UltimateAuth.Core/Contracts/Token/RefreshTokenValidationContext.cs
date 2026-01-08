using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record RefreshTokenValidationContext
    {
        public string TenantId { get; init; } = default!;
        public AuthSessionId SessionId { get; init; }
        public string ProvidedRefreshToken { get; init; } = default!;
        public DateTimeOffset Now { get; init; }
    }
}
