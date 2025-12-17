namespace CodeBeam.UltimateAuth.Core.Domain
{
    public sealed class UAuthSessionRoot<TUserId> : ISessionRoot<TUserId>
    {
        public TUserId UserId { get; }
        public string? TenantId { get; }
        public bool IsRevoked { get; }
        public DateTimeOffset? RevokedAt { get; }
        public long SecurityVersion { get; }
        public IReadOnlyList<ISessionChain<TUserId>> Chains { get; }
        public DateTimeOffset LastUpdatedAt { get; }

        private UAuthSessionRoot(
            string? tenantId,
            TUserId userId,
            bool isRevoked,
            DateTimeOffset? revokedAt,
            long securityVersion,
            IReadOnlyList<ISessionChain<TUserId>> chains,
            DateTimeOffset lastUpdatedAt)
        {
            TenantId = tenantId;
            UserId = userId;
            IsRevoked = isRevoked;
            RevokedAt = revokedAt;
            SecurityVersion = securityVersion;
            Chains = chains;
            LastUpdatedAt = lastUpdatedAt;
        }

        public static ISessionRoot<TUserId> Create(
            string? tenantId,
            TUserId userId,
            DateTimeOffset issuedAt)
        {
            return new UAuthSessionRoot<TUserId>(
                tenantId,
                userId,
                isRevoked: false,
                revokedAt: null,
                securityVersion: 0,
                chains: Array.Empty<ISessionChain<TUserId>>(),
                lastUpdatedAt: issuedAt
            );
        }

        public ISessionRoot<TUserId> Revoke(DateTimeOffset at)
        {
            if (IsRevoked)
                return this;

            return new UAuthSessionRoot<TUserId>(
                TenantId,
                UserId,
                isRevoked: true,
                revokedAt: at,
                securityVersion: SecurityVersion,
                chains: Chains,
                lastUpdatedAt: at
            );
        }

        public ISessionRoot<TUserId> AttachChain(ISessionChain<TUserId> chain, DateTimeOffset at)
        {
            if (IsRevoked)
                return this;

            return new UAuthSessionRoot<TUserId>(
                TenantId,
                UserId,
                IsRevoked,
                RevokedAt,
                SecurityVersion,
                Chains.Concat(new[] { chain }).ToArray(),
                at
            );
        }

    }
}
