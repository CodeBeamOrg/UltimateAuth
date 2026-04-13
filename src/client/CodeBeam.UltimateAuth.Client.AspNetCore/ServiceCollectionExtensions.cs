using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Client.AspNetCore;

public static class UAuthAspNetCoreExtensions
{
    public static IServiceCollection AddUltimateAuthAspNetCoreCompatibility(
        this IServiceCollection services)
    {
        services.AddAuthentication("UAuth.Noop")
            .AddScheme<AuthenticationSchemeOptions, NoopAuthHandler>("UAuth.Noop", _ => { });

        services.AddAuthorization();

        return services;
    }
}
