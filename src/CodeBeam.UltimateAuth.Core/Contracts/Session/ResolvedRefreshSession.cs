using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record ResolvedRefreshSession<TUserId>
    {
        public bool IsValid { get; init; }
        public bool IsReuseDetected { get; init; }

        public ISession<TUserId>? Session { get; init; }
        public ISessionChain<TUserId>? Chain { get; init; }

        private ResolvedRefreshSession() { }

        public static ResolvedRefreshSession<TUserId> Invalid()
            => new()
            {
                IsValid = false
            };

        public static ResolvedRefreshSession<TUserId> Reused()
            => new()
            {
                IsValid = false,
                IsReuseDetected = true
            };

        public static ResolvedRefreshSession<TUserId> Valid(
            ISession<TUserId> session,
            ISessionChain<TUserId> chain)
            => new()
            {
                IsValid = true,
                Session = session,
                Chain = chain
            };
    }
}
