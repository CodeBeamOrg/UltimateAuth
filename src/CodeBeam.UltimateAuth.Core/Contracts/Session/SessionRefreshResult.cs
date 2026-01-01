using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record SessionRefreshResult
    {
        public SessionRefreshStatus Status { get; init; }

        public PrimaryToken? PrimaryToken { get; init; }

        public RefreshToken? RefreshToken { get; init; }

        public bool DidTouch { get; init; }

        public bool IsSuccess => Status == SessionRefreshStatus.Success;

        private SessionRefreshResult() { }

        public static SessionRefreshResult Success(
            PrimaryToken primaryToken,
            RefreshToken? refreshToken = null,
            bool didTouch = false)
            => new()
            {
                Status = SessionRefreshStatus.Success,
                PrimaryToken = primaryToken,
                RefreshToken = refreshToken,
                DidTouch = didTouch
            };

        public static SessionRefreshResult ReauthRequired()
        => new()
        {
            Status = SessionRefreshStatus.ReauthRequired
        };

        public static SessionRefreshResult InvalidRequest()
            => new()
            {
                Status = SessionRefreshStatus.InvalidRequest
            };

        public static SessionRefreshResult Failed()
        => new()
        {
            Status = SessionRefreshStatus.Failed
        };

        public bool RequiresReauth => Status == SessionRefreshStatus.ReauthRequired;

    }
}
