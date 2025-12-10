namespace CodeBeam.UltimateAuth.Core.Domain
{
    /// <summary>
    /// Represents the effective runtime state of an authentication session.
    /// Evaluated based on expiration rules, revocation status, and security version checks.
    /// </summary>
    public enum SessionState
    {
        /// <summary>
        /// The session is valid, not expired, not revoked, and its security version
        /// matches the user's current security version.
        /// </summary>
        Active = 0,

        /// <summary>
        /// The session has passed its expiration time and is no longer valid.
        /// </summary>
        Expired = 1,

        /// <summary>
        /// The session was explicitly revoked by user action or administrative control.
        /// </summary>
        Revoked = 2,

        /// <summary>
        /// The session's parent chain has been revoked, typically representing a
        /// device-level logout or device ban.
        /// </summary>
        ChainRevoked = 3,

        /// <summary>
        /// The user's entire session root has been revoked. This invalidates all
        /// chains and sessions immediately across all devices.
        /// </summary>
        RootRevoked = 4,

        /// <summary>
        /// The session's stored SecurityVersionAtCreation does not match the user's
        /// current security version, indicating a password reset, MFA reset,
        /// or other critical security event.
        /// </summary>
        SecurityVersionMismatch = 5
    }
}
