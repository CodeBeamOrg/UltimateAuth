using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Credentials.Reference
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUltimateAuthCredentialsReference(this IServiceCollection services)
        {
            services.TryAddScoped<IUserCredentialsService, DefaultUserCredentialsService>();

            return services;
        }
    }
}
