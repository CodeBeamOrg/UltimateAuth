namespace CodeBeam.UltimateAuth.Core.Domain
{
    public sealed class UAuthSessionRoot<TUserId> : ISessionRoot<TUserId>
    {
        public TUserId UserId { get; }
        public string? TenantId { get; }
        public bool IsRevoked { get; }
        public DateTime? RevokedAt { get; }
        public long SecurityVersion { get; }
        public IReadOnlyList<ISessionChain<TUserId>> Chains { get; }
        public DateTime LastUpdatedAt { get; }

        private UAuthSessionRoot(
            string? tenantId,
            TUserId userId,
            bool isRevoked,
            DateTime? revokedAt,
            long securityVersion,
            IReadOnlyList<ISessionChain<TUserId>> chains,
            DateTime lastUpdatedAt)
        {
            TenantId = tenantId;
            UserId = userId;
            IsRevoked = isRevoked;
            RevokedAt = revokedAt;
            SecurityVersion = securityVersion;
            Chains = chains;
            LastUpdatedAt = lastUpdatedAt;
        }

        public static UAuthSessionRoot<TUserId> Create(
            string? tenantId,
            TUserId userId,
            DateTime issuedAt)
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

        public UAuthSessionRoot<TUserId> Revoke(DateTime at)
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

    }
}
