using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record SessionRefreshResult
    {
        public SessionRefreshStatus Status { get; init; }

        public AccessToken? AccessToken { get; init; }

        public RefreshToken? RefreshToken { get; init; }

        public bool IsSuccess => Status == SessionRefreshStatus.Success;

        private SessionRefreshResult() { }

        public static SessionRefreshResult Success(
            AccessToken accessToken,
            RefreshToken? refreshToken)
            => new()
            {
                Status = SessionRefreshStatus.Success,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

        public static SessionRefreshResult ReauthRequired()
            => new()
            {
                Status = SessionRefreshStatus.ReauthRequired
            };

        // TODO: ?
        public static SessionRefreshResult Invalid() => new();
    }
}
