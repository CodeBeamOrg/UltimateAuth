namespace CodeBeam.UltimateAuth.Core.Contracts;

public enum LogoutReason
{
    Explicit,
    SessionExpired,
    SecurityPolicy,
    AdminForced,
    TenantDisabled
}
