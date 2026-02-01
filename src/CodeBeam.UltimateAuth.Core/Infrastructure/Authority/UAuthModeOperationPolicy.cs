using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class AuthModeOperationPolicy : IAuthorityPolicy
{
    public bool AppliesTo(AuthContext context) => true; // Applies to all contexts

    public AccessDecisionResult Decide(AuthContext context)
    {
        return context.Mode switch
        {
            UAuthMode.PureOpaque => DecideForPureOpaque(context),
            UAuthMode.PureJwt => DecideForPureJwt(context),
            UAuthMode.Hybrid => AccessDecisionResult.Allow(),
            UAuthMode.SemiHybrid => AccessDecisionResult.Allow(),

            _ => AccessDecisionResult.Deny("Unsupported authentication mode.")
        };
    }

    private static AccessDecisionResult DecideForPureOpaque(AuthContext context)
    {
        if (context.Operation == AuthOperation.Refresh)
            return AccessDecisionResult.Deny("Refresh operation is not supported in PureOpaque mode.");

        return AccessDecisionResult.Allow();
    }

    private static AccessDecisionResult DecideForPureJwt(AuthContext context)
    {
        if (context.Operation == AuthOperation.Access)
            return AccessDecisionResult.Deny("Session-based access is not supported in PureJwt mode.");

        return AccessDecisionResult.Allow();
    }
}
