namespace CodeBeam.UltimateAuth.Core.Contracts
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
