namespace CodeBeam.UltimateAuth.Core.Contracts;

public enum TokenInvalidReason
{
    Invalid = 0,
    Expired = 10,
    Revoked = 20,
    Malformed = 30,
    SignatureInvalid = 40,
    AudienceMismatch = 50,
    IssuerMismatch = 60,
    MissingSubject = 70,
    NotImplemented = 80,
    Unknown = 100
}
