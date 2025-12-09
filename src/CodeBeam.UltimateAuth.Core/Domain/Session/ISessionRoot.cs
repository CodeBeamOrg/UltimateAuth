namespace CodeBeam.UltimateAuth.Core.Domain
{
    public interface ISessionRoot<TUserId>
    {
        /// <summary>
        /// Owner of all session chains.
        /// </summary>
        TUserId UserId { get; }

        /// <summary>
        /// Global revoke flag — invalidates all chains and sessions.
        /// </summary>
        bool IsRevoked { get; }
        DateTime? RevokedAt { get; }

        /// <summary>
        /// Global security version — incremented on password/MFA reset, etc.
        /// Used to invalidate all sessions even if not revoked individually.
        /// </summary>
        long SecurityVersion { get; }

        /// <summary>
        /// Complete set of device/login families.
        /// (Immutable — mutations go through SessionStore/SessionService)
        /// </summary>
        IReadOnlyList<ISessionChain<TUserId>> Chains { get; }

        /// <summary>
        /// Last time this structure was updated.
        /// Helps with caching and concurrency controls.
        /// </summary>
        DateTime LastUpdatedAt { get; }
    }
}
