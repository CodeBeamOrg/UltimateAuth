namespace CodeBeam.UltimateAuth.Core
{
    public enum TokenInvalidReason
    {
        Invalid,
        Expired,
        Revoked,
        Malformed,
        SignatureInvalid,
        AudienceMismatch,
        IssuerMismatch,
        MissingSubject,
        Unknown,
        NotImplemented
    }
}
