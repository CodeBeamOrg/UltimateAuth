namespace CodeBeam.UltimateAuth.Core.Domain
{
    /// <summary>
    /// Represents the effective runtime state of an authentication session.
    /// Evaluated based on expiration rules, revocation status, and security version checks.
    /// </summary>
    public enum SessionState
    {
        Active,
        Expired,
        Revoked,
        NotFound,
        Invalid,
        SecurityMismatch,
        DeviceMismatch
    }
}
