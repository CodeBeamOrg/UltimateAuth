namespace CodeBeam.UltimateAuth.Core.Domain
{
    public sealed class UAuthSessionChain<TUserId> : ISessionChain<TUserId>
    {
        public ChainId ChainId { get; }
        public string? TenantId { get; }
        public TUserId UserId { get; }
        public int RotationCount { get; }
        public long SecurityVersionAtCreation { get; }
        public ClaimsSnapshot ClaimsSnapshot { get; }
        public AuthSessionId? ActiveSessionId { get; }
        public bool IsRevoked { get; }
        public DateTime? RevokedAt { get; }

        private UAuthSessionChain(
            ChainId chainId,
            string? tenantId,
            TUserId userId,
            int rotationCount,
            long securityVersionAtCreation,
            ClaimsSnapshot claimsSnapshot,
            AuthSessionId? activeSessionId,
            bool isRevoked,
            DateTime? revokedAt)
        {
            ChainId = chainId;
            TenantId = tenantId;
            UserId = userId;
            RotationCount = rotationCount;
            SecurityVersionAtCreation = securityVersionAtCreation;
            ClaimsSnapshot = claimsSnapshot;
            ActiveSessionId = activeSessionId;
            IsRevoked = isRevoked;
            RevokedAt = revokedAt;
        }

        public static UAuthSessionChain<TUserId> Create(
            ChainId chainId,
            string? tenantId,
            TUserId userId,
            long securityVersion,
            ClaimsSnapshot claimsSnapshot)
        {
            return new UAuthSessionChain<TUserId>(
                chainId,
                tenantId,
                userId,
                rotationCount: 0,
                securityVersionAtCreation: securityVersion,
                claimsSnapshot: claimsSnapshot,
                activeSessionId: null,
                isRevoked: false,
                revokedAt: null
            );
        }

        public ISessionChain<TUserId> AttachSession(AuthSessionId sessionId)
        {
            if (IsRevoked)
                return this;

            return new UAuthSessionChain<TUserId>(
                ChainId,
                TenantId,
                UserId,
                RotationCount, // Unchanged on first attach
                SecurityVersionAtCreation,
                ClaimsSnapshot,
                activeSessionId: sessionId,
                isRevoked: false,
                revokedAt: null
            );
        }

        public ISessionChain<TUserId> RotateSession(AuthSessionId sessionId)
        {
            if (IsRevoked)
                return this;

            return new UAuthSessionChain<TUserId>(
                ChainId,
                TenantId,
                UserId,
                RotationCount + 1,
                SecurityVersionAtCreation,
                ClaimsSnapshot,
                activeSessionId: sessionId,
                isRevoked: false,
                revokedAt: null
            );
        }

        public ISessionChain<TUserId> Revoke(DateTime at)
        {
            if (IsRevoked)
                return this;

            return new UAuthSessionChain<TUserId>(
                ChainId,
                TenantId,
                UserId,
                RotationCount,
                SecurityVersionAtCreation,
                ClaimsSnapshot,
                ActiveSessionId,
                isRevoked: true,
                revokedAt: at
            );
        }

    }
}
