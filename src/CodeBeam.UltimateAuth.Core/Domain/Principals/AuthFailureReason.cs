namespace CodeBeam.UltimateAuth.Core.Domain
{
    public enum AuthFailureReason
    {
        InvalidCredentials,
        LockedOut,
        RequiresMfa,
        SessionExpired,
        SessionRevoked,
        TenantDisabled,
        Unauthorized,
        Unknown
    }
}
