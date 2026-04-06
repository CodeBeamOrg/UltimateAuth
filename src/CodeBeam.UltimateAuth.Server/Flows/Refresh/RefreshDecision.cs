namespace CodeBeam.UltimateAuth.Server.Flows;

/// <summary>
/// Determines which authentication artifacts can be refreshed
/// for the current AuthMode.
/// This is a server-side decision and must be enforced centrally.
/// </summary>
public enum RefreshDecision
{
    /// <summary>
    /// Refresh endpoint is disabled for this mode.
    /// </summary>
    NotSupported = 0,

    /// <summary>
    /// Only session lifetime is extended.
    /// No access / refresh token issued.
    /// (PureOpaque)
    /// </summary>
    SessionTouch = 10,

    /// <summary>
    /// Refresh token is rotated and
    /// a new access token is issued.
    /// Session MAY also be touched depending on policy.
    /// (Hybrid, SemiHybrid, PureJwt)
    /// </summary>
    TokenRotation = 20
}
