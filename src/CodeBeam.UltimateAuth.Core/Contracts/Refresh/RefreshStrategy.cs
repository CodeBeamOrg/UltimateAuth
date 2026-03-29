namespace CodeBeam.UltimateAuth.Core.Contracts;

public enum RefreshStrategy
{
    NotSupported = 0,
    SessionOnly = 10,           // PureOpaque
    TokenOnly = 20,             // PureJwt
    TokenWithSessionCheck = 30, // SemiHybrid
    SessionAndToken = 40        // Hybrid
}
