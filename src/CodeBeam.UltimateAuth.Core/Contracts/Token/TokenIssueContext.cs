using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record TokenIssueContext<TUserId>
    {
        public string? TenantId { get; init; }
        public ISession<TUserId> Session { get; init; } = default!;
        public DateTime Now { get; init; }
    }
}
