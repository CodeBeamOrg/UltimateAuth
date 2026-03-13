using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Flows;

public sealed class SessionTouchService : ISessionTouchService
{
    private readonly ISessionStoreFactory _kernelFactory;

    public SessionTouchService(ISessionStoreFactory kernelFactory)
    {
        _kernelFactory = kernelFactory;
    }

    // It's designed for PureOpaque sessions, which do not issue new refresh tokens on refresh.
    // That's why the service access store direcly: There is no security flow here, only validate and touch session.
    public async Task<SessionRefreshResult> RefreshAsync(SessionValidationResult validation, SessionTouchPolicy policy, SessionTouchMode sessionTouchMode, DateTimeOffset now, CancellationToken ct = default)
    {
        if (!validation.IsValid || validation.ChainId is null)
            return SessionRefreshResult.ReauthRequired();

        if (!policy.TouchInterval.HasValue)
            return SessionRefreshResult.Success(validation.SessionId!.Value, didTouch: false);

        var kernel = _kernelFactory.Create(validation.Tenant);

        bool didTouch = false;

        await kernel.ExecuteAsync(async _ =>
        {
            var chain = await kernel.GetChainAsync(validation.ChainId.Value);

            if (chain is null || chain.IsRevoked)
                return;

            if (now - chain.LastSeenAt < policy.TouchInterval.Value)
                return;

            var expectedVersion = chain.Version;
            var touched = chain.Touch(now);

            await kernel.SaveChainAsync(touched, expectedVersion);
            didTouch = true;
        }, ct);

        return SessionRefreshResult.Success(validation.SessionId!.Value, didTouch);
    }
}
