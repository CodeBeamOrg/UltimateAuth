using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record TokenIssuanceContext
{
    public required UserKey UserKey { get; init; }
    public string? TenantId { get; init; }
    public IReadOnlyDictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();
    public AuthSessionId? SessionId { get; init; }
    public SessionChainId? ChainId { get; init; }
    public DateTimeOffset IssuedAt { get; init; }
}
