namespace CodeBeam.UltimateAuth.Core.Domain
{
    public enum SessionState
    {
        Active = 0,
        Expired = 1,
        Revoked = 2,
        ChainRevoked = 3,
        RootRevoked = 4,
        SecurityVersionMismatch = 5
    }
}
