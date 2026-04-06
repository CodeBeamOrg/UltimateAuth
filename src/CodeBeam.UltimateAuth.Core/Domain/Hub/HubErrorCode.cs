namespace CodeBeam.UltimateAuth.Core.Domain;

public enum HubErrorCode
{
    None = 0,
    InvalidCredentials = 10,
    LockedOut = 20,
    RequiresMfa = 30,
    Unknown = 100
}
