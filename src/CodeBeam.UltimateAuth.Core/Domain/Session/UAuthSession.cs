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
        public ClaimsSnapshot Claims { get; }
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
        ClaimsSnapshot claims,
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
            Claims = claims;
            Metadata = metadata;
        }

        public static UAuthSession<TUserId> Create(
            AuthSessionId sessionId,
            string? tenantId,
            TUserId userId,
            DateTime now,
            DateTime expiresAt,
            DeviceInfo device,
            ClaimsSnapshot claims,
            SessionMetadata metadata)
        {
            return new(
                sessionId,
                tenantId,
                userId,
                createdAt: now,
                expiresAt: expiresAt,
                lastSeenAt: now,
                isRevoked: false,
                revokedAt: null,
                securityVersionAtCreation: 0,
                device: device,
                claims: claims,
                metadata: metadata
            );
        }

        public UAuthSession<TUserId> WithSecurityVersion(long version)
        {
            if (SecurityVersionAtCreation == version)
                return this;

            return new UAuthSession<TUserId>(
                SessionId,
                TenantId,
                UserId,
                CreatedAt,
                ExpiresAt,
                LastSeenAt,
                IsRevoked,
                RevokedAt,
                version,
                Device,
                Claims,
                Metadata
            );
        }

        public bool ShouldUpdateLastSeen(DateTime now)
        {
            if (LastSeenAt is null)
                return true;

            return (now - LastSeenAt.Value) >= TimeSpan.FromMinutes(1);
        }

        public ISession<TUserId> Touch(DateTime now)
        {
            if (!ShouldUpdateLastSeen(now))
                return this;

            return new UAuthSession<TUserId>(
                SessionId,
                TenantId,
                UserId,
                CreatedAt,
                ExpiresAt,
                now,
                IsRevoked,
                RevokedAt,
                SecurityVersionAtCreation,
                Device,
                Claims,
                Metadata
            );
        }

        public UAuthSession<TUserId> Revoke(DateTime at)
        {
            if (IsRevoked) return this;

            return new(
                SessionId,
                TenantId,
                UserId,
                CreatedAt,
                ExpiresAt,
                LastSeenAt,
                true,
                at,
                SecurityVersionAtCreation,
                Device,
                Claims,
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
