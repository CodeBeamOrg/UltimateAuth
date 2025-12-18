namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public enum RefreshTokenFailureReason
    {
        Invalid,
        Expired,
        Revoked,
        Reused
    }
}
