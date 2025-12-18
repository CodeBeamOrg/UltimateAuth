namespace CodeBeam.UltimateAuth.Core.Domain.Session
{
    public sealed class UAuthSession<TUserId> : ISession<TUserId>
    {
        public AuthSessionId SessionId { get; }
        public string? TenantId { get; }
        public TUserId UserId { get; }
        public ChainId ChainId { get; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset ExpiresAt { get; }
        public DateTimeOffset? LastSeenAt { get; }
        public bool IsRevoked { get; }
        public DateTimeOffset? RevokedAt { get; }
        public long SecurityVersionAtCreation { get; }
        public DeviceInfo Device { get; }
        public ClaimsSnapshot Claims { get; }
        public SessionMetadata Metadata { get; }

        private UAuthSession(
        AuthSessionId sessionId,
        string? tenantId,
        TUserId userId,
        ChainId chainId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        DateTimeOffset? lastSeenAt,
        bool isRevoked,
        DateTimeOffset? revokedAt,
        long securityVersionAtCreation,
        DeviceInfo device,
        ClaimsSnapshot claims,
        SessionMetadata metadata)
        {
            SessionId = sessionId;
            TenantId = tenantId;
            UserId = userId;
            ChainId = chainId;
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
            ChainId chainId,
            DateTimeOffset now,
            DateTimeOffset expiresAt,
            DeviceInfo device,
            ClaimsSnapshot claims,
            SessionMetadata metadata)
        {
            return new(
                sessionId,
                tenantId,
                userId,
                chainId,
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
                ChainId,
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

        public bool ShouldUpdateLastSeen(DateTimeOffset at)
        {
            if (LastSeenAt is null)
                return true;

            return (at - LastSeenAt.Value) >= TimeSpan.FromMinutes(1);
        }

        public ISession<TUserId> Touch(DateTimeOffset at)
        {
            if (!ShouldUpdateLastSeen(at))
                return this;

            return new UAuthSession<TUserId>(
                SessionId,
                TenantId,
                UserId,
                ChainId,
                CreatedAt,
                ExpiresAt,
                at,
                IsRevoked,
                RevokedAt,
                SecurityVersionAtCreation,
                Device,
                Claims,
                Metadata
            );
        }

        public ISession<TUserId> Revoke(DateTimeOffset at)
        {
            if (IsRevoked) return this;

            return new UAuthSession<TUserId>(
                SessionId,
                TenantId,
                UserId,
                ChainId,
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

        public SessionState GetState(DateTimeOffset at)
        {
            if (IsRevoked) return SessionState.Revoked;
            if (at >= ExpiresAt) return SessionState.Expired;
            return SessionState.Active;
        }
    }

}
