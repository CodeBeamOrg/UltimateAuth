using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Core.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Extensions;

/// <summary>
/// Provides extension methods for registering UltimateAuth core services into the application's dependency injection container.
/// 
/// These methods configure options, validators, converters, and factories required for the authentication subsystem.
/// 
/// IMPORTANT:
/// This extension registers only CORE services — session stores, token factories, PKCE handlers, and any server-specific 
/// logic must be added from the Server package (e.g., AddUltimateAuthServer()).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuth(this IServiceCollection services, Action<UAuthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<UAuthOptions>();

        if (configure is not null)
        {
            optionsBuilder.Configure<DirectCoreConfigurationMarker>((options, marker) =>
            {
                marker.MarkConfigured();
                configure(options);
            });
        }

        optionsBuilder.BindConfiguration("UltimateAuth:Core");

        services.TryAddSingleton<IPostConfigureOptions<UAuthOptions>>(sp =>
        {
            var marker = sp.GetRequiredService<DirectCoreConfigurationMarker>();
            var config = sp.GetService<IConfiguration>();

            return new CoreConfigurationIntentDetector(marker, config);
        });

        return services.AddUltimateAuthInternal();
    }

    /// <summary>
    /// Internal shared registration pipeline invoked by all AddUltimateAuth overloads.
    /// Registers validators, user ID converters, and placeholder factories.
    /// Core-level invariant validation.
    /// Server layer may add additional validators.
    /// NOTE:
    /// This method does not register session stores or server-side services.
    /// A server project must explicitly call:
    /// "services.AddUltimateAuthSessionStore'TStore'();"
    /// to provide a concrete ISessionStore implementation.
    /// </summary>
    private static IServiceCollection AddUltimateAuthInternal(this IServiceCollection services)
    {
        services.TryAddSingleton<DirectCoreConfigurationMarker>();
        services.AddSingleton<IPostConfigureOptions<UAuthOptions>, UAuthOptionsPostConfigureGuard>();


        services.AddSingleton<IValidateOptions<UAuthOptions>, UAuthOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthSessionOptions>, UAuthSessionOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthTokenOptions>, UAuthTokenOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthLoginOptions>, UAuthLoginOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthPkceOptions>, UAuthPkceOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthMultiTenantOptions>, UAuthMultiTenantOptionsValidator>();

        services.AddSingleton<IUserIdConverterResolver, UAuthUserIdConverterResolver>();
        services.TryAddSingleton<IUAuthProductInfoProvider, UAuthProductInfoProvider>();
        services.TryAddSingleton<IUserAgentParser, UAuthUserAgentParser>();

        return services;
    }
}
