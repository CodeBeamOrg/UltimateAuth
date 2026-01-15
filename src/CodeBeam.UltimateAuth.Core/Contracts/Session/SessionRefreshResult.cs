using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record SessionRefreshResult
    {
        public SessionRefreshStatus Status { get; init; }

        public AuthSessionId? SessionId { get; init; }

        public bool DidTouch { get; init; }

        public bool IsSuccess => Status == SessionRefreshStatus.Success;
        public bool RequiresReauth => Status == SessionRefreshStatus.ReauthRequired;

        private SessionRefreshResult() { }

        public static SessionRefreshResult Success(
            AuthSessionId sessionId,
            bool didTouch = false)
            => new()
            {
                Status = SessionRefreshStatus.Success,
                SessionId = sessionId,
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

    }
}
