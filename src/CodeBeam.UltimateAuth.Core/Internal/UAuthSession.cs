using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Internal
{
    internal sealed class UAuthSession<TUserId> : ISession<TUserId>
    {
        public UAuthSession(AuthSessionId sessionId, TUserId userId, DateTime createdAt, DateTime expiresAt, DateTime lastSeenAt,
                            long securityVersionAtCreation, DeviceInfo device, SessionMetadata metadata, bool isRevoked = false,
                            DateTime? revokedAt = null)
        {
            SessionId = sessionId;
            UserId = userId;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
            LastSeenAt = lastSeenAt;
            SecurityVersionAtCreation = securityVersionAtCreation;
            Device = device;
            Metadata = metadata;
            IsRevoked = isRevoked;
            RevokedAt = revokedAt;
        }

        public AuthSessionId SessionId { get; }
        public TUserId UserId { get; }

        public DateTime CreatedAt { get; }
        public DateTime ExpiresAt { get; }
        public DateTime LastSeenAt { get; }

        public long SecurityVersionAtCreation { get; }

        public bool IsRevoked { get; }
        public DateTime? RevokedAt { get; }

        public DeviceInfo Device { get; }
        public SessionMetadata Metadata { get; }

        public SessionState GetState(DateTime now)
        {
            if (IsRevoked) return SessionState.Revoked;
            if (now >= ExpiresAt) return SessionState.Expired;

            return SessionState.Active;
        }

        public static UAuthSession<TUserId> CreateNew(TUserId userId, long rootSecurityVersion, DeviceInfo device, SessionMetadata metadata, DateTime now, TimeSpan lifetime)
        {
            return new UAuthSession<TUserId>(
                sessionId: AuthSessionId.New(),
                userId: userId,
                createdAt: now,
                expiresAt: now.Add(lifetime),
                lastSeenAt: now,
                securityVersionAtCreation: rootSecurityVersion,
                device: device,
                metadata: metadata
            );
        }

        // TODO: WithUpdatedLastSeenAt? Add as optionally used in session validation flow.
        public UAuthSession<TUserId> WithRevoked(DateTime at)
        {
            return new UAuthSession<TUserId>(
                SessionId, UserId, CreatedAt, ExpiresAt, LastSeenAt,
                SecurityVersionAtCreation, Device, Metadata,
                isRevoked: true,
                revokedAt: at
            );
        }
    }
}
