namespace CodeBeam.UltimateAuth.Core.Domain;

public enum HubErrorCode
{
    None = 0,
    InvalidCredentials,
    LockedOut,
    RequiresMfa,
    Unknown
}
