namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public enum RefreshStrategy
    {
        NotSupported,
        SessionOnly,           // PureOpaque
        TokenOnly,             // PureJwt
        TokenWithSessionCheck, // SemiHybrid
        SessionAndToken        // Hybrid
    }
}
