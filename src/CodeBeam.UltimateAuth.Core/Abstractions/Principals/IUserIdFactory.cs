namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Responsible for creating new user identifiers.
    /// This abstraction allows UltimateAuth to remain
    /// independent from the concrete user ID type.
    /// </summary>
    /// <typeparam name="TUserId">User identifier type.</typeparam>
    public interface IUserIdFactory<TUserId>
    {
        /// <summary>
        /// Creates a new unique user identifier.
        /// </summary>
        TUserId Create();
    }
}
