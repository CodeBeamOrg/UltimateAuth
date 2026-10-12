using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

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
    public async Task<SessionRefreshResult> RefreshAsync(SessionValidationResult validation, SessionTouchPolicy policy, SessionTouchMode sessionTouchMode,
                                            DateTimeOffset now, CancellationToken ct = default)
    {
        if (!validation.IsValid || validation.SessionId is null || validation.ChainId is null)
        {
            return SessionRefreshResult.ReauthRequired();
        }

        var kernel = _kernelFactory.Create(validation.Tenant);

        bool isValid = false;
        bool didTouch = false;

        await kernel.ExecuteAsync(async innerCt =>
        {
            var chain = await kernel.GetChainAsync(validation.ChainId.Value, innerCt);
            var session = await kernel.GetSessionAsync(validation.SessionId.Value, innerCt);

            if (chain is null || session is null)
                return;

            if (chain.GetState(now, policy.IdleTimeout) != SessionState.Active || session.GetState(now) != SessionState.Active)
                return;

            if (chain.ActiveSessionId != session.SessionId ||
                session.ChainId != chain.ChainId ||
                session.Tenant != validation.Tenant ||
                chain.Tenant != validation.Tenant ||
                session.UserKey != chain.UserKey)
                return;

            if (validation.UserKey != session.UserKey || validation.ChainId != session.ChainId)
                return;

            isValid = true;

            bool shouldTouch = sessionTouchMode == SessionTouchMode.Force || !policy.TouchInterval.HasValue || now - chain.LastSeenAt >= policy.TouchInterval.Value;

            if (!shouldTouch)
                return;

            var touchedChain = chain.Touch(now);

            if (policy.SlidingExpiration)
            {
                var proposedExpiry = now.Add(policy.Lifetime);

                var newExpiry = chain.AbsoluteExpiresAt is { } absolute
                    && proposedExpiry > absolute
                        ? absolute
                        : proposedExpiry;

                var extendedSession = session.ExtendExpiration(newExpiry);

                if (!ReferenceEquals(extendedSession, session))
                {
                    await kernel.SaveSessionAsync(extendedSession, session.Version, innerCt);
                }
            }

            await kernel.SaveChainAsync(touchedChain, chain.Version, innerCt);

            didTouch = true;
        }, ct);

        return isValid
            ? SessionRefreshResult.Success(validation.SessionId.Value, didTouch)
            : SessionRefreshResult.ReauthRequired();
    }
}
