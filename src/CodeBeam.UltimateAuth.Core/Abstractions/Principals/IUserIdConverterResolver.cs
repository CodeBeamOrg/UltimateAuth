namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Resolves the appropriate <see cref="IUserIdConverter{TUserId}"/> instance
    /// for a given user identifier type. Used internally by UltimateAuth to
    /// ensure consistent serialization and parsing of user IDs across all components.
    /// </summary>
    public interface IUserIdConverterResolver
    {
        /// <summary>
        /// Retrieves the registered <see cref="IUserIdConverter{TUserId}"/> for the specified user ID type.
        /// </summary>
        /// <typeparam name="TUserId">The type of the user identifier.</typeparam>
        /// <returns>
        /// A converter capable of transforming the user ID to and from its string
        /// and binary representations.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no converter has been registered for the requested user ID type.
        /// </exception>
        IUserIdConverter<TUserId> GetConverter<TUserId>(string? purpose = null);
    }
}
