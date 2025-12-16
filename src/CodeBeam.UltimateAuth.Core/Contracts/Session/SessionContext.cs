using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    /// <summary>
    /// Lightweight session context resolved from the incoming request.
    /// Does NOT load or validate the session.
    /// Used only by middleware and engines as input.
    /// </summary>
    public sealed class SessionContext
    {
        public AuthSessionId? SessionId { get; }
        public string? TenantId { get; }

        public bool IsAnonymous => SessionId is null;

        private SessionContext(AuthSessionId? sessionId, string? tenantId)
        {
            SessionId = sessionId;
            TenantId = tenantId;
        }

        public static SessionContext Anonymous()
            => new(null, null);

        public static SessionContext FromSessionId(
            AuthSessionId sessionId,
            string? tenantId)
            => new(sessionId, tenantId);
    }
}
