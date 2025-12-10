using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Events
{
    public sealed class SessionCreatedContext<TUserId> : IAuthEventContext
    {
        public TUserId UserId { get; }
        public AuthSessionId SessionId { get; }
        public ChainId ChainId { get; }
        public DateTime CreatedAt { get; }

        public SessionCreatedContext(TUserId userId, AuthSessionId sessionId, ChainId chainId, DateTime createdAt)
        {
            UserId = userId;
            SessionId = sessionId;
            ChainId = chainId;
            CreatedAt = createdAt;
        }

    }
}
