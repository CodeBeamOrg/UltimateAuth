namespace CodeBeam.UltimateAuth.Core.Contracts;

public enum LogoutReason
{
    UserIntend = 0,
    Explicit = 10,
    SessionExpired = 20,
    SecurityPolicy = 30,
    AdminForced = 40,
    TenantDisabled = 50
}
