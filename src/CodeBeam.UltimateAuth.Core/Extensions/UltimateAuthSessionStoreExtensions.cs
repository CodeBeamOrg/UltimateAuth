using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Core.Extensions
{
    /// <summary>
    /// Provides extension methods for registering a concrete <see cref="ISessionStoreKernel{TUserId}"/>
    /// implementation into the application's dependency injection container.
    /// 
    /// UltimateAuth requires exactly one session store implementation that determines
    /// how sessions, chains, and roots are persisted (e.g., EF Core, Dapper, Redis, MongoDB).
    /// This extension performs automatic generic type resolution and registers the correct
    /// ISessionStore&lt;TUserId&gt; for the application's user ID type.
    /// 
    /// The method enforces that the provided store implements ISessionStore'TUserId';.
    /// If the type cannot be determined, an exception is thrown to prevent misconfiguration.
    /// </summary>
    public static class UltimateAuthSessionStoreExtensions
    {
        /// <summary>
        /// Registers a custom session store implementation for UltimateAuth.
        /// The supplied <typeparamref name="TStore"/> must implement ISessionStore'TUserId';
        /// exactly once with a single TUserId generic argument.
        /// 
        /// After registration, the internal session store factory resolves the correct
        /// ISessionStore instance at runtime for the active tenant and TUserId type.
        /// </summary>
        /// <typeparam name="TStore">The concrete session store implementation.</typeparam>
        public static IServiceCollection AddUltimateAuthSessionStore<TStore>(this IServiceCollection services)
            where TStore : class
        {
            var storeInterface = typeof(TStore)
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ISessionStoreKernel<>));

            if (storeInterface is null)
            {
                throw new InvalidOperationException(
                    $"{typeof(TStore).Name} must implement ISessionStoreKernel<TUserId>.");
            }

            var userIdType = storeInterface.GetGenericArguments()[0];
            var typedInterface = typeof(ISessionStoreKernel<>).MakeGenericType(userIdType);

            services.TryAddScoped(typedInterface, typeof(TStore));

            services.AddSingleton<ISessionStoreFactory>(sp =>
                new GenericSessionStoreFactory(sp, userIdType));

            return services;
        }
    }

    /// <summary>
    /// Default session store factory used by UltimateAuth to dynamically create
    /// the correct ISessionStore&lt;TUserId&gt; implementation at runtime.
    ///
    /// This factory ensures type safety by validating the requested TUserId against
    /// the registered session store’s user ID type. Attempting to resolve a mismatched
    /// TUserId results in a descriptive exception to prevent silent misconfiguration.
    /// 
    /// Tenant ID is passed through so that multi-tenant implementations can perform
    /// tenant-aware routing, filtering, or partition-based selection.
    /// </summary>
    internal sealed class GenericSessionStoreFactory : ISessionStoreFactory
    {
        private readonly IServiceProvider _sp;
        private readonly Type _userIdType;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericSessionStoreFactory"/> class.
        /// </summary>
        public GenericSessionStoreFactory(IServiceProvider sp, Type userIdType)
        {
            _sp = sp;
            _userIdType = userIdType;
        }

        /// <summary>
        /// Creates and returns the registered ISessionStore&lt;TUserId&gt; implementation
        /// for the specified tenant and user ID type.
        /// Throws if the requested TUserId does not match the registered store's type.
        /// </summary>
        public ISessionStoreKernel<TUserId> Create<TUserId>(string? tenantId)
        {
            if (typeof(TUserId) != _userIdType)
            {
                throw new InvalidOperationException(
                    $"SessionStore registered for TUserId='{_userIdType.Name}', " +
                    $"but requested with TUserId='{typeof(TUserId).Name}'.");
            }

            var typed = typeof(ISessionStoreKernel<>).MakeGenericType(_userIdType);
            var store = _sp.GetRequiredService(typed);

            return (ISessionStoreKernel<TUserId>)store;
        }
    }
}
