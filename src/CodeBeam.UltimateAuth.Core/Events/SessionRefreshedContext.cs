using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Events
{
    public sealed class SessionRefreshedContext<TUserId> : IAuthEventContext
    {
        public TUserId UserId { get; }
        public AuthSessionId OldSessionId { get; }
        public AuthSessionId NewSessionId { get; }
        public ChainId ChainId { get; }
        public DateTime RefreshedAt { get; }

        public SessionRefreshedContext(TUserId userId, AuthSessionId oldSessionId, AuthSessionId newSessionId, ChainId chainId, DateTime refreshedAt)
        {
            UserId = userId;
            OldSessionId = oldSessionId;
            NewSessionId = newSessionId;
            ChainId = chainId;
            RefreshedAt = refreshedAt;
        }

    }
}
