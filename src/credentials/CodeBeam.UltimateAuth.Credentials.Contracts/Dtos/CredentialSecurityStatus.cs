namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public enum CredentialSecurityStatus
{
    Active = 0,

    Revoked = 10,
    Locked = 20,
    Expired = 30,
    ResetRequested = 40,
}
