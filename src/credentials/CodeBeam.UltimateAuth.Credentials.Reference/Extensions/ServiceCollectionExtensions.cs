using CodeBeam.UltimateAuth.Credentials.Reference.Internal;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Credentials.Reference
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUltimateAuthCredentialsReference(this IServiceCollection services)
        {
            services.TryAddScoped<ICredentialManagementService, CredentialManagementService>();
            services.TryAddScoped<IUserCredentialsInternalService, CredentialManagementService>();
            services.TryAddScoped<ICredentialEndpointHandler, CredentialEndpointHandler>();
            services.TryAddScoped<IUserLifecycleIntegration, PasswordUserLifecycleIntegration>();
            services.AddScoped<ICredentialProvider, PasswordCredentialProvider>();
            return services;
        }
    }
}
