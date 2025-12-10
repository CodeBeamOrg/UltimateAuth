using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Internal
{
    internal sealed class UAuthSessionRoot<TUserId> : ISessionRoot<TUserId>
    {
        public UAuthSessionRoot(string? tenantId, TUserId userId, bool isRevoked, DateTime? revokedAt, long securityVersion, IReadOnlyList<ISessionChain<TUserId>> chains, DateTime lastUpdatedAt)
        {
            TenantId = tenantId;
            UserId = userId;
            IsRevoked = isRevoked;
            RevokedAt = revokedAt;
            SecurityVersion = securityVersion;
            Chains = chains;
            LastUpdatedAt = lastUpdatedAt;
        }

        public string? TenantId { get; }
        public TUserId UserId { get; }
        public bool IsRevoked { get; }
        public DateTime? RevokedAt { get; }
        public long SecurityVersion { get; }
        public IReadOnlyList<ISessionChain<TUserId>> Chains { get; }
        public DateTime LastUpdatedAt { get; }

        public static UAuthSessionRoot<TUserId> CreateNew(string? tenantId, TUserId userId, DateTime now)
        {
            return new UAuthSessionRoot<TUserId>(
                tenantId: tenantId,
                userId: userId,
                isRevoked: false,
                revokedAt: null,
                securityVersion: 1,
                chains: Array.Empty<ISessionChain<TUserId>>(),
                lastUpdatedAt: now
            );
        }

        public UAuthSessionRoot<TUserId> AddChain(ISessionChain<TUserId> chain, DateTime now)
        {
            var newList = new List<ISessionChain<TUserId>>(Chains.Count + 1);
            newList.AddRange(Chains);
            newList.Add(chain);

            return new UAuthSessionRoot<TUserId>(
                TenantId,
                UserId,
                IsRevoked,
                RevokedAt,
                SecurityVersion,
                newList,
                lastUpdatedAt: now
            );
        }

        public UAuthSessionRoot<TUserId> WithSecurityVersionIncrement(DateTime now)
        {
            return new UAuthSessionRoot<TUserId>(
                TenantId,
                UserId,
                IsRevoked,
                RevokedAt,
                securityVersion: SecurityVersion + 1,
                Chains,
                lastUpdatedAt: now
            );
        }

        public UAuthSessionRoot<TUserId> WithRevoked(DateTime at)
        {
            return new UAuthSessionRoot<TUserId>(
                TenantId,
                UserId,
                isRevoked: true,
                revokedAt: at,
                SecurityVersion,
                Chains,
                lastUpdatedAt: at
            );
        }

        public UAuthSessionRoot<TUserId> WithUnrevoked(DateTime now)
        {
            return new UAuthSessionRoot<TUserId>(
                TenantId,
                UserId,
                isRevoked: false,
                revokedAt: null,
                SecurityVersion,
                Chains,
                lastUpdatedAt: now
            );
        }
    }
}
