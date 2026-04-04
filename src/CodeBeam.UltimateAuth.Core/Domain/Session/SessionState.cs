namespace CodeBeam.UltimateAuth.Core.Domain;

/// <summary>
/// Represents the effective runtime state of an authentication session.
/// Evaluated based on expiration rules, revocation status, and security version checks.
/// </summary>
public enum SessionState
{
    Active = 0,
    Expired = 10,
    Revoked = 20,
    NotFound = 30,
    Invalid = 40,
    SecurityMismatch = 50,
    DeviceMismatch = 60,
    Unsupported = 100
}
