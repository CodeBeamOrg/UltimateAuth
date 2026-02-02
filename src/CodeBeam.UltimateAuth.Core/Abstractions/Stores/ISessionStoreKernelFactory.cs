using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Provides a factory abstraction for creating tenant-scoped session store
/// instances capable of persisting sessions, chains, and session roots.
/// Implementations typically resolve concrete <see cref="ISessionStoreKernel"/> types from the dependency injection container.
/// </summary>
public interface ISessionStoreKernelFactory
{
    /// <summary>
    /// Creates and returns a session store instance for the specified user ID type within the given tenant context.
    /// </summary>
    /// <returns>
    /// An <see cref="ISessionStoreKernel"/> implementation able to perform session persistence operations.
    /// </returns>
    ISessionStoreKernel Create(TenantKey tenant);
}
