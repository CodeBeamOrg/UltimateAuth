using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record ReauthRequest
    {
        public string? TenantId { get; init; }
        public AuthSessionId SessionId { get; init; }
        public string Secret { get; init; } = default!;
    }
}
