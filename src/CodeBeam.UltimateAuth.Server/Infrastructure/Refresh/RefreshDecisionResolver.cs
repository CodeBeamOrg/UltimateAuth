using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    /// <summary>
    /// Resolves refresh behavior based on AuthMode and server options.
    /// This class is the single source of truth for refresh capability.
    /// </summary>
    public static class RefreshDecisionResolver
    {
        public static RefreshDecision Resolve(UAuthServerOptions options)
        {
            return options.Mode switch
            {
                UAuthMode.PureOpaque => RefreshDecision.SessionOnly,
                UAuthMode.Hybrid => RefreshDecision.SessionAndToken,
                UAuthMode.SemiHybrid => RefreshDecision.TokenOnly,
                UAuthMode.PureJwt => RefreshDecision.TokenOnly,

                _ => RefreshDecision.NotSupported
            };
        }
    }
}
