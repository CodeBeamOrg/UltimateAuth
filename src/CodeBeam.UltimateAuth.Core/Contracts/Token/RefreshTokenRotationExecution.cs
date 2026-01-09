using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record RefreshTokenRotationExecution<TUserId>
    {
        public RefreshTokenRotationResult Result { get; init; } = default!;

        // INTERNAL – flow/orchestrator only
        public TUserId? UserId { get; init; }
        public AuthSessionId? SessionId { get; init; }
        public ChainId? ChainId { get; init; }
        public string? TenantId { get; init; }
    }
}
