namespace CodeBeam.UltimateAuth.Server.Security;

internal readonly record struct UAuthCsrfTransport(bool CanIssueAuthenticationCookie, bool CanConsumeAuthenticationCookie, bool IsVerifiedHeaderOnly)
{
    public bool RequiresProtection(UAuthCsrfOperation operation)
    {
        return operation switch
        {
            UAuthCsrfOperation.Login or UAuthCsrfOperation.PkceCompletion
                => CanIssueAuthenticationCookie,

            UAuthCsrfOperation.AuthenticatedMutation
                => CanConsumeAuthenticationCookie || CanIssueAuthenticationCookie,

            _ => true
        };
    }
}
