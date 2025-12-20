using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed record TokenIssuanceContext
    {
        public string UserId { get; init; } = default!;
        public string? TenantId { get; init; }
        public IReadOnlyDictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();
        public string? SessionId { get; init; }
        public DateTimeOffset IssuedAt { get; init; }
    }
}
