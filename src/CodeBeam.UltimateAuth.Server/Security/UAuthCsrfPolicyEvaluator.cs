using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Server.Security;

internal sealed class UAuthCsrfPolicyEvaluator
{
    public bool RequiresProtection(AuthFlowContext auth, UAuthCsrfOperation operation)
    {
        return RequiresProtection(operation,
            UAuthCsrfResponseInspector.CanIssueAuthenticationCookie(auth.Response),
            auth.Options.SessionResolution.EnableCookie);
    }

    internal static bool RequiresProtection(UAuthCsrfOperation operation, bool canIssueCookie, bool canAcceptCookie)
    {
        return operation switch
        {
            UAuthCsrfOperation.Login
                or UAuthCsrfOperation.PkceCompletion
                or UAuthCsrfOperation.AuthenticatedMutation
                => canIssueCookie || canAcceptCookie,

            _ => true
        };
    }
}
