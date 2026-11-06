namespace CodeBeam.UltimateAuth.Server.Security;

public enum UAuthCsrfOperation
{
    Login = 0,
    PkceCompletion = 10,
    AuthenticatedMutation = 20
}
