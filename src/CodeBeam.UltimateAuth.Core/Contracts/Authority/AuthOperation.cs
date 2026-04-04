namespace CodeBeam.UltimateAuth.Core.Contracts;

public enum AuthOperation
{
    Login = 0,
    Access = 10,
    ResourceAccess = 20,
    Refresh = 30,
    Revoke = 40,
    Logout = 50,
    System = 100,
}
