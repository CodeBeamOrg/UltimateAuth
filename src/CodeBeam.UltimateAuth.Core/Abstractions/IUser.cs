namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Represents the minimal user abstraction required by UltimateAuth.
    /// Includes the unique user identifier and an optional set of claims that
    /// may be used during authentication or session creation.
    /// </summary>
    public interface IUser<TUserId>
    {
        /// <summary>
        /// Gets the unique identifier of the user.
        /// </summary>
        TUserId UserId { get; }

        /// <summary>
        /// Gets an optional collection of user claims that may be used to construct
        /// session-level claim snapshots. Implementations may return <c>null</c> if no claims are available.
        /// </summary>
        IReadOnlyDictionary<string, object>? Claims { get; }
    }
}
