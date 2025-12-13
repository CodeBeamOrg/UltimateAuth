using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contexts
{
    /// <summary>
    /// Represents the context in which a session is issued
    /// (login, refresh, reauthentication).
    /// </summary>
    public sealed class SessionIssueContext<TUserId>
    {
        public required TUserId UserId { get; init; }
        public string? TenantId { get; init; }

        public required long SecurityVersion { get; init; }

        public DeviceInfo Device { get; init; }
        public IReadOnlyDictionary<string, object>? ClaimsSnapshot { get; init; }

        public DateTime Now { get; init; } = DateTime.UtcNow;

        public ChainId? ChainId { get; init; }
    }
}
