using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class DefaultSessionTouchService : ISessionTouchService
    {
        private readonly ISessionStore _sessionStore;

        public DefaultSessionTouchService(ISessionStore sessionStore)
        {
            _sessionStore = sessionStore;
        }

        // It's designed for PureOpaque sessions, which do not issue new refresh tokens on refresh.
        // That's why the service access store direcly: There is no security flow here, only validate and touch session.
        public async Task<SessionRefreshResult> RefreshAsync(SessionValidationResult validation, SessionTouchPolicy policy, SessionTouchMode sessionTouchMode, DateTimeOffset now, CancellationToken ct = default)
        {
            if (!validation.IsValid)
                return SessionRefreshResult.ReauthRequired();

            //var session = validation.Session;
            bool didTouch = false;

            if (policy.TouchInterval.HasValue)
            {
                //var elapsed = now - session.LastSeenAt;

                //if (elapsed >= policy.TouchInterval.Value)
                //{
                //    var touched = session.Touch(now);
                //    await _activityWriter.TouchAsync(validation.TenantId, touched, ct);
                //    didTouch = true;
                //}

                didTouch = await _sessionStore.TouchSessionAsync(validation.TenantId, validation.SessionId.Value, now, sessionTouchMode, ct);
            }

            return SessionRefreshResult.Success(validation.SessionId.Value, didTouch);
        }
    }
}
