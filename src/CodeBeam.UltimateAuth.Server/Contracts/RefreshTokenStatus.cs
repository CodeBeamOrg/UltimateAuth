namespace CodeBeam.UltimateAuth.Server.Contracts
{
    public enum RefreshTokenStatus
    {
        Valid = 0,
        Expired = 1,
        Revoked = 2,
        NotFound = 3,
        Reused = 4,
        SessionMismatch = 5
    }
}
