using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Internal
{
    internal sealed class UAuthSessionChain<TUserId> : ISessionChain<TUserId>
    {
        public UAuthSessionChain(ChainId chainId, TUserId userId, int rotationCount, long securityVersionAtCreation, IReadOnlyDictionary<string, object>? claimsSnapshot,
                                 IReadOnlyList<ISession<TUserId>> sessions, bool isRevoked = false, DateTime? revokedAt = null)
        {
            ChainId = chainId;
            UserId = userId;
            RotationCount = rotationCount;
            SecurityVersionAtCreation = securityVersionAtCreation;
            ClaimsSnapshot = claimsSnapshot;
            Sessions = sessions;
            IsRevoked = isRevoked;
            RevokedAt = revokedAt;
        }

        public ChainId ChainId { get; }
        public TUserId UserId { get; }

        public int RotationCount { get; }
        public long SecurityVersionAtCreation { get; }

        public IReadOnlyDictionary<string, object>? ClaimsSnapshot { get; }
        public IReadOnlyList<ISession<TUserId>> Sessions { get; }

        public bool IsRevoked { get; }
        public DateTime? RevokedAt { get; }

        public static UAuthSessionChain<TUserId> CreateNew(TUserId userId, long rootSecurityVersion, ISession<TUserId> initialSession, IReadOnlyDictionary<string, object>? claimsSnapshot)
        {
            return new UAuthSessionChain<TUserId>(
                chainId: ChainId.New(),
                userId: userId,
                rotationCount: 0,
                securityVersionAtCreation: rootSecurityVersion,
                claimsSnapshot: claimsSnapshot,
                sessions: new[] { initialSession }
            );
        }

        public UAuthSessionChain<TUserId> AddRotatedSession(ISession<TUserId> session)
        {
            var newList = new List<ISession<TUserId>>(Sessions.Count + 1);
            newList.AddRange(Sessions);
            newList.Add(session);

            return new UAuthSessionChain<TUserId>(
                ChainId,
                UserId,
                RotationCount + 1,
                SecurityVersionAtCreation,
                ClaimsSnapshot,
                newList,
                IsRevoked,
                RevokedAt
            );
        }

        public UAuthSessionChain<TUserId> WithRevoked(DateTime at)
        {
            return new UAuthSessionChain<TUserId>(
                ChainId,
                UserId,
                RotationCount,
                SecurityVersionAtCreation,
                ClaimsSnapshot,
                Sessions,
                isRevoked: true,
                revokedAt: at
            );
        }
    }
}
