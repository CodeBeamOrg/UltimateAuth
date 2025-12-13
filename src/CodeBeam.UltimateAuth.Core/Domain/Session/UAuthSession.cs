namespace CodeBeam.UltimateAuth.Core.Domain.Session
{
    public sealed class UAuthSession<TUserId> : ISession<TUserId>
    {
        public AuthSessionId SessionId { get; }
        public string? TenantId { get; }
        public TUserId UserId { get; }
        public DateTime CreatedAt { get; }
        public DateTime ExpiresAt { get; }
        public DateTime? LastSeenAt { get; }
        public bool IsRevoked { get; }
        public DateTime? RevokedAt { get; }
        public long SecurityVersionAtCreation { get; }
        public DeviceInfo Device { get; }
        public SessionMetadata Metadata { get; }

        private UAuthSession(
            AuthSessionId sessionId,
            string? tenantId,
            TUserId userId,
            DateTime createdAt,
            DateTime expiresAt,
            DateTime? lastSeenAt,
            bool isRevoked,
            DateTime? revokedAt,
            long securityVersionAtCreation,
            DeviceInfo device,
            SessionMetadata metadata)
        {
            SessionId = sessionId;
            TenantId = tenantId;
            UserId = userId;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
            LastSeenAt = lastSeenAt;
            IsRevoked = isRevoked;
            RevokedAt = revokedAt;
            SecurityVersionAtCreation = securityVersionAtCreation;
            Device = device;
            Metadata = metadata;
        }

        public static UAuthSession<TUserId> Create(
            AuthSessionId sessionId,
            string? tenantId,
            TUserId userId,
            DateTime now,
            DateTime expiresAt,
            long securityVersion,
            DeviceInfo device,
            SessionMetadata metadata)
        {
            return new UAuthSession<TUserId>(
                sessionId,
                tenantId,
                userId,
                createdAt: now,
                expiresAt: expiresAt,
                lastSeenAt: now,
                isRevoked: false,
                revokedAt: null,
                securityVersionAtCreation: securityVersion,
                device: device,
                metadata: metadata
            );
        }

        public UAuthSession<TUserId> WithLastSeen(DateTime now)
        {
            return new UAuthSession<TUserId>(
                SessionId,
                TenantId,
                UserId,
                CreatedAt,
                ExpiresAt,
                lastSeenAt: now,
                IsRevoked,
                RevokedAt,
                SecurityVersionAtCreation,
                Device,
                Metadata
            );
        }

        public UAuthSession<TUserId> Revoke(DateTime at)
        {
            if (IsRevoked) return this;

            return new UAuthSession<TUserId>(
                SessionId,
                TenantId,
                UserId,
                CreatedAt,
                ExpiresAt,
                LastSeenAt,
                isRevoked: true,
                revokedAt: at,
                SecurityVersionAtCreation,
                Device,
                Metadata
            );
        }

        public SessionState GetState(DateTime now)
        {
            if (IsRevoked) return SessionState.Revoked;
            if (now >= ExpiresAt) return SessionState.Expired;
            return SessionState.Active;
        }
    }

}
