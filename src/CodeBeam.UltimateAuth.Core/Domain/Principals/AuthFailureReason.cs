namespace CodeBeam.UltimateAuth.Core.Domain;

public enum AuthFailureReason
{
    InvalidCredentials = 0,
    LockedOut = 10,
    RequiresMfa = 20,
    ReauthenticationRequired = 30,
    Unauthorized = 40,
    SessionExpired = 100,
    SessionRevoked = 110,
    TenantDisabled = 120,
    Unknown = 1000
}
