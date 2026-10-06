using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;

internal static class ServiceCollectionTestExtensions
{
    public static void DecorateForTest<TService>(this IServiceCollection services, Func<IServiceProvider, TService, TService> decorator) where TService : class
    {
        var descriptor = services.LastOrDefault(x => x.ServiceType == typeof(TService));

        if (descriptor is null)
        {
            throw new InvalidOperationException($"Service '{typeof(TService).FullName}' is not registered.");
        }

        services.Remove(descriptor);

        services.Add(
            ServiceDescriptor.Describe(typeof(TService),
                sp =>
                {
                    var inner = CreateInstance<TService>(sp, descriptor);

                    return decorator(sp, inner);
                },
                descriptor.Lifetime));
    }

    private static TService CreateInstance<TService>(IServiceProvider serviceProvider, ServiceDescriptor descriptor) where TService : class
    {
        if (descriptor.ImplementationInstance is TService instance)
            return instance;

        if (descriptor.ImplementationFactory is not null)
        {
            return (TService)descriptor.ImplementationFactory(serviceProvider);
        }

        if (descriptor.ImplementationType is not null)
        {
            return (TService)ActivatorUtilities.CreateInstance(serviceProvider,descriptor.ImplementationType);
        }

        throw new InvalidOperationException($"Unable to construct decorated service '{typeof(TService).FullName}'.");
    }
}
