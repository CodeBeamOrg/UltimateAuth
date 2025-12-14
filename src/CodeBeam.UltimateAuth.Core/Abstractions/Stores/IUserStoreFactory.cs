namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Provides a factory abstraction for creating tenant-scoped user store
    /// instances used for retrieving basic user information required by
    /// UltimateAuth authentication services.
    /// </summary>
    public interface IUserStoreFactory
    {
        /// <summary>
        /// Creates and returns a user store instance for the specified user ID type within the given tenant context.
        /// </summary>
        /// <typeparam name="TUserId">The type used to uniquely identify users.</typeparam>
        /// <param name="tenantId">
        /// The tenant identifier for multi-tenant environments, or <c>null</c>
        /// in single-tenant deployments.
        /// </param>
        /// <returns>
        /// An <see cref="IUAuthUserStore{TUserId}"/> implementation capable of user lookup and security metadata retrieval.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no user store implementation has been registered for the given user ID type.
        /// </exception>
        IUAuthUserStore<TUserId> Create<TUserId>(string tenantId);
    }
}
