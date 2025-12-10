namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Provides a factory abstraction for creating tenant-scoped session store
    /// instances capable of persisting sessions, chains, and session roots.
    /// Implementations typically resolve concrete <see cref="ISessionStore{TUserId}"/> types from the dependency injection container.
    /// </summary>
    public interface ISessionStoreFactory
    {
        /// <summary>
        /// Creates and returns a session store instance for the specified user ID type within the given tenant context.
        /// </summary>
        /// <typeparam name="TUserId">The type used to uniquely identify users.</typeparam>
        /// <param name="tenantId">
        /// The tenant identifier for multi-tenant environments, or <c>null</c> for single-tenant mode.
        /// </param>
        /// <returns>
        /// An <see cref="ISessionStore{TUserId}"/> implementation able to perform session persistence operations.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no compatible session store implementation is registered.
        /// </exception>
        ISessionStore<TUserId> Create<TUserId>(string? tenantId);
    }
}
