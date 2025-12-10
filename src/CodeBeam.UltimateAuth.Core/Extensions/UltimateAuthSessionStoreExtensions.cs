using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Core.Extensions
{
    public static class UltimateAuthSessionStoreExtensions
    {
        public static IServiceCollection AddUltimateAuthSessionStore<TStore>(this IServiceCollection services) where TStore : class
        {
            var storeInterface = typeof(TStore)
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ISessionStore<>));

            if (storeInterface is null)
            {
                throw new InvalidOperationException(
                    $"{typeof(TStore).Name} must implement ISessionStore<TUserId>.");
            }

            var userIdType = storeInterface.GetGenericArguments()[0];
            var typedInterface = typeof(ISessionStore<>).MakeGenericType(userIdType);
            services.TryAddScoped(typedInterface, typeof(TStore));

            services.AddSingleton<ISessionStoreFactory>(sp => new GenericSessionStoreFactory(sp, userIdType));

            return services;
        }
    }

    internal sealed class GenericSessionStoreFactory : ISessionStoreFactory
    {
        private readonly IServiceProvider _sp;
        private readonly Type _userIdType;

        public GenericSessionStoreFactory(IServiceProvider sp, Type userIdType)
        {
            _sp = sp;
            _userIdType = userIdType;
        }

        public ISessionStore<TUserId> Create<TUserId>(string tenantId)
        {
            if (typeof(TUserId) != _userIdType)
            {
                throw new InvalidOperationException(
                    $"SessionStore registered for TUserId='{_userIdType.Name}', " +
                    $"but requested with TUserId='{typeof(TUserId).Name}'.");
            }

            var typed = typeof(ISessionStore<>).MakeGenericType(_userIdType);
            var store = _sp.GetRequiredService(typed);

            return (ISessionStore<TUserId>)store;
        }
    }
}
