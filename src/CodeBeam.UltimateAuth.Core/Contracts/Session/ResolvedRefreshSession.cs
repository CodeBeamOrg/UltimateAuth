using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record ResolvedRefreshSession
    {
        public bool IsValid { get; init; }
        public bool IsReuseDetected { get; init; }

        public ISession? Session { get; init; }
        public ISessionChain? Chain { get; init; }

        private ResolvedRefreshSession() { }

        public static ResolvedRefreshSession Invalid()
            => new()
            {
                IsValid = false
            };

        public static ResolvedRefreshSession Reused()
            => new()
            {
                IsValid = false,
                IsReuseDetected = true
            };

        public static ResolvedRefreshSession Valid(
            ISession session,
            ISessionChain chain)
            => new()
            {
                IsValid = true,
                Session = session,
                Chain = chain
            };
    }
}
