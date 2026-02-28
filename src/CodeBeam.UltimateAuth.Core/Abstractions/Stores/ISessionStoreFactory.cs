using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Provides a factory abstraction for creating tenant-scoped session store
/// instances capable of persisting sessions, chains, and session roots.
/// Implementations typically resolve concrete <see cref="ISessionStore"/> types from the dependency injection container.
/// </summary>
public interface ISessionStoreFactory
{
    /// <summary>
    /// Creates and returns a session store instance for the specified user ID type within the given tenant context.
    /// </summary>
    /// <returns>
    /// An <see cref="ISessionStore"/> implementation able to perform session persistence operations.
    /// </returns>
    ISessionStore Create(TenantKey tenant);
}
