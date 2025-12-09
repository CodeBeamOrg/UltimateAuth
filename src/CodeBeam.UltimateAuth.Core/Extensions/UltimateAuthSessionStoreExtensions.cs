using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Core.Extensions
{
    public static class UltimateAuthSessionStoreExtensions
    {
        /// <summary>
        /// Registers a custom session store implementation for UltimateAuth.
        /// TStore must implement ISessionStore&lt;TUserId&gt;.
        ///
        /// Example:
        ///     services.AddUltimateAuthSessionStore&lt;MyEfStore&lt;Guid&gt;&gt;();
        /// </summary>
        public static IServiceCollection AddUltimateAuthSessionStore<TStore>(this IServiceCollection services)
            where TStore : class
        {
            // 1) Identify TUserId by scanning interface
            var storeInterface = typeof(TStore)
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ISessionStore<>));

            if (storeInterface is null)
                throw new InvalidOperationException(
                    $"{typeof(TStore).Name} must implement ISessionStore<TUserId>.");

            var userIdType = storeInterface.GetGenericArguments()[0];

            // 2) Register concrete instance mapped to ISessionStore<TUserId>
            var typedInterface = typeof(ISessionStore<>).MakeGenericType(userIdType);

            services.TryAddScoped(typedInterface, typeof(TStore));

            // 3) Override the factory so SessionService can resolve correct store
            services.AddSingleton<ISessionStoreFactory>(sp =>
                new GenericSessionStoreFactory(sp, typedInterface, userIdType));

            return services;
        }
    }

    internal sealed class GenericSessionStoreFactory : ISessionStoreFactory
    {
        private readonly IServiceProvider _sp;
        private readonly Type _typedStoreInterface;
        private readonly Type _userIdType;

        public GenericSessionStoreFactory(
            IServiceProvider sp,
            Type typedStoreInterface,
            Type userIdType)
        {
            _sp = sp;
            _typedStoreInterface = typedStoreInterface;
            _userIdType = userIdType;
        }

        public object CreateStore(Type userIdType)
        {
            if (userIdType != _userIdType)
            {
                throw new InvalidOperationException(
                    $"SessionStore requested for TUserId='{userIdType.Name}', " +
                    $"but the registered store only supports TUserId='{_userIdType.Name}'.");
            }

            return _sp.GetRequiredService(_typedStoreInterface);
        }
    }
}
