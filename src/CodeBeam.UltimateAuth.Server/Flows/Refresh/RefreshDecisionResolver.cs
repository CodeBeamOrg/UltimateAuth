using CodeBeam.UltimateAuth.Core;

namespace CodeBeam.UltimateAuth.Server.Flows;

/// <summary>
/// Resolves refresh behavior based on AuthMode.
/// This class is the single source of truth for refresh capability.
/// </summary>
public static class RefreshDecisionResolver
{
    public static RefreshDecision Resolve(UAuthMode mode)
    {
        return mode switch
        {
            UAuthMode.PureOpaque => RefreshDecision.SessionTouch,

            UAuthMode.Hybrid
            or UAuthMode.SemiHybrid
            or UAuthMode.PureJwt => RefreshDecision.TokenRotation,

            _ => RefreshDecision.NotSupported
        };
    }
}
