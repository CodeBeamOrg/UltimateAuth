namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    /// <summary>
    /// Determines which authentication artifacts can be refreshed
    /// for the current AuthMode.
    /// This is a server-side decision and must be enforced centrally.
    /// </summary>
    public enum RefreshDecision
    {
        /// <summary>
        /// Refresh is not supported for this mode.
        /// </summary>
        NotSupported = 0,

        /// <summary>
        /// Only session lifecycle can be refreshed.
        /// (PureOpaque)
        /// </summary>
        SessionOnly = 1,

        /// <summary>
        /// Session lifecycle + token issuance can be refreshed.
        /// (Hybrid)
        /// </summary>
        SessionAndToken = 2,

        /// <summary>
        /// Only token lifecycle can be refreshed.
        /// (SemiHybrid, PureJwt)
        /// </summary>
        TokenOnly = 3
    }
}
