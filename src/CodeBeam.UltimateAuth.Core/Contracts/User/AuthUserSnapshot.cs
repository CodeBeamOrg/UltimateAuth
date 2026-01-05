namespace CodeBeam.UltimateAuth.Core.Contracts
{
    // This is for AuthFlowContext, with minimal data and no db access
    public sealed class AuthUserSnapshot<TUserId>
    {
        public bool IsAuthenticated { get; }
        public TUserId? UserId { get; }

        private AuthUserSnapshot(bool isAuthenticated, TUserId? userId)
        {
            IsAuthenticated = isAuthenticated;
            UserId = userId;
        }

        public static AuthUserSnapshot<TUserId> Authenticated(TUserId userId) => new(true, userId);
        public static AuthUserSnapshot<TUserId> Anonymous() => new(false, default);
    }
}
