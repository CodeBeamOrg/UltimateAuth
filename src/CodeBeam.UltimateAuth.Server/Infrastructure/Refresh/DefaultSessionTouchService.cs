using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TUserId"></typeparam>
    public sealed class DefaultSessionTouchService<TUserId> : ISessionTouchService<TUserId> where TUserId : notnull
    {
        private readonly ISessionActivityWriter<TUserId> _activityWriter;

        public DefaultSessionTouchService(ISessionActivityWriter<TUserId> activityWriter)
        {
            _activityWriter = activityWriter;
        }

        // It's designed for PureOpaque sessions, which do not issue new refresh tokens on refresh.
        // That's why the service access store direcly: There is no security flow here, only validate and touch session.
        public async Task<SessionRefreshResult> RefreshAsync(SessionValidationResult<TUserId> validation, SessionTouchPolicy policy, DateTimeOffset now, CancellationToken ct = default)
        {
            if (!validation.IsValid)
                return SessionRefreshResult.ReauthRequired();

            var session = validation.Session;
            bool didTouch = false;

            if (policy.TouchInterval.HasValue)
            {
                var elapsed = now - session.LastSeenAt;

                if (elapsed >= policy.TouchInterval.Value)
                {
                    var touched = session.Touch(now);
                    await _activityWriter.TouchAsync(validation.TenantId, touched, ct);
                    didTouch = true;
                }
            }

            return SessionRefreshResult.Success(session.SessionId, didTouch);
        }
    }
}
