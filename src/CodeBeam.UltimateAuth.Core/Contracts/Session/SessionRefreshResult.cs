namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record SessionRefreshResult
    {
        public AccessToken AccessToken { get; init; } = default!;
        public RefreshToken? RefreshToken { get; init; }

        public bool IsValid => AccessToken is not null;

        private SessionRefreshResult() { }

        public static SessionRefreshResult Success(AccessToken accessToken, RefreshToken? refreshToken)
            => new()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

        public static SessionRefreshResult Invalid() => new();
    }
}
