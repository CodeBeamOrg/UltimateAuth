using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Events
{
    public sealed class SessionRevokedContext<TUserId> : IAuthEventContext
    {
        public TUserId UserId { get; }
        public AuthSessionId SessionId { get; }
        public ChainId ChainId { get; }
        public DateTime RevokedAt { get; }

        public SessionRevokedContext(TUserId userId, AuthSessionId sessionId, ChainId chainId, DateTime revokedAt)
        {
            UserId = userId;
            SessionId = sessionId;
            ChainId = chainId;
            RevokedAt = revokedAt;
        }

    }
}
