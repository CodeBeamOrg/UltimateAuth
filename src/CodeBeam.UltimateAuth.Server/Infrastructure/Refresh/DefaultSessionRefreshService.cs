using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TUserId"></typeparam>
    public sealed class DefaultSessionRefreshService<TUserId> : ISessionRefreshService<TUserId> where TUserId : notnull
    {
        private readonly UAuthServerOptions _options;
        private readonly ISessionStore<TUserId> _store;
        private readonly ISessionActivityWriter<TUserId> _activityWriter;

        public DefaultSessionRefreshService(
            IOptions<UAuthServerOptions> options,
            ISessionStore<TUserId> store,
            ISessionActivityWriter<TUserId> activityWriter)
        {
            _options = options.Value;
            _store = store;
            _activityWriter = activityWriter;
        }

        // It's designed for PureOpaque sessions, which do not issue new refresh tokens on refresh.
        // That's why the service access store direcly: There is no security flow here, only validate and touch session.
        public async Task<SessionRefreshResult> RefreshAsync(SessionValidationResult<TUserId> validation, DateTimeOffset now, CancellationToken ct = default)
        {
            if (!validation.IsValid)
                return SessionRefreshResult.ReauthRequired();

            var session = validation.Session;
            bool didTouch = false;
            var touchInterval = _options.Session.TouchInterval;

            if (touchInterval.HasValue)
            {
                var elapsed = now - session.LastSeenAt;

                if (elapsed >= touchInterval.Value)
                {
                    var touched = session.Touch(now);
                    await _activityWriter.TouchAsync(validation.TenantId, touched, ct);
                    didTouch = true;
                }
            }

            var primaryToken = PrimaryToken.FromSession(session.SessionId);
            // For PureOpaque sessions, we do not issue a new refresh token on refresh.
            return SessionRefreshResult.Success(primaryToken, didTouch: didTouch);
        }
    }
}
