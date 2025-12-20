using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Composition.Extensions;

public static class AddUltimateAuthServerExtensions
{
    public static UltimateAuthServerBuilder AddUltimateAuthServer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddUltimateAuth(configuration); // Core
        services.AddUAuthServerInfrastructure(); // issuer, flow, endpoints

        services.Configure<UAuthServerOptions>(configuration.GetSection("UltimateAuth:Server"));

        return new UltimateAuthServerBuilder(services);
    }
}
