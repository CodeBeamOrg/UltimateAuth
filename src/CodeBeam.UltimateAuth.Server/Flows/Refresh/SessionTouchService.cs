using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Flows;

public sealed class SessionTouchService : ISessionTouchService
{
    private readonly ISessionStoreKernelFactory _kernelFactory;

    public SessionTouchService(ISessionStoreKernelFactory kernelFactory)
    {
        _kernelFactory = kernelFactory;
    }

    // It's designed for PureOpaque sessions, which do not issue new refresh tokens on refresh.
    // That's why the service access store direcly: There is no security flow here, only validate and touch session.
    public async Task<SessionRefreshResult> RefreshAsync(SessionValidationResult validation, SessionTouchPolicy policy, SessionTouchMode sessionTouchMode, DateTimeOffset now, CancellationToken ct = default)
    {
        if (!validation.IsValid || validation.SessionId is null)
            return SessionRefreshResult.ReauthRequired();

        if (!policy.TouchInterval.HasValue)
            return SessionRefreshResult.Success(validation.SessionId.Value, didTouch: false);

        var kernel = _kernelFactory.Create(validation.Tenant);

        bool didTouch = false;

        await kernel.ExecuteAsync(async _ =>
        {
            var session = await kernel.GetSessionAsync(validation.SessionId.Value);
            if (session is null || session.IsRevoked)
                return;

            if (sessionTouchMode == SessionTouchMode.IfNeeded && now - session.LastSeenAt < policy.TouchInterval.Value)
                return;

            var touched = session.Touch(now);
            await kernel.SaveSessionAsync(touched);
            didTouch = true;
        }, ct);

        return SessionRefreshResult.Success(validation.SessionId.Value, didTouch);
    }
}
