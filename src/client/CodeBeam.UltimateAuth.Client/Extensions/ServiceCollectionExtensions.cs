using CodeBeam.UltimateAuth.Client.Authentication;
using CodeBeam.UltimateAuth.Client.Device;
using CodeBeam.UltimateAuth.Client.Devices;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Runtime;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Extensions;

/// <summary>
/// Provides extension methods for registering UltimateAuth client services.
///
/// This layer is responsible for:
/// - Client-side authentication actions (login, logout, refresh, reauth)
/// - Browser-based POST infrastructure (JS form submit)
/// - Endpoint configuration for auth mutations
///
/// This extension can safely be used together with AddUltimateAuthServer()
/// in Blazor Server applications.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers core UltimateAuth client services.
    /// <code>
    /// This package contains the platform-agnostic authentication engine:
    /// - Authentication flows (login, logout, refresh, PKCE)
    /// - Client state management
    /// - Domain-level services (User, Session, Credential, Authorization)
    /// </code>
    /// 
    /// IMPORTANT:
    /// This package does NOT include any platform-specific implementations such as:
    /// - HTTP / JS transport
    /// - Storage (browser, mobile, etc.)
    /// - UI integrations
    /// 
    /// To use this in an application, you must install a client adapter package
    /// such as:
    /// - CodeBeam.UltimateAuth.Client.Blazor
    /// - (future) CodeBeam.UltimateAuth.Client.Maui
    /// 
    /// These adapter packages provide concrete implementations for:
    /// - Request transport
    /// - Storage
    /// - Device identification
    /// - Navigation / UI integration
    /// </summary>
    public static IServiceCollection AddUltimateAuthClient(this IServiceCollection services, Action<UAuthClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ClientConfigurationMarker>();

        services.AddOptions<UAuthClientOptions>()
            .Configure<ClientConfigurationMarker>((options, marker) =>
            {
                if (configure != null)
                {
                    marker.MarkConfigured();
                    configure(options);
                }
            })
            .BindConfiguration("UltimateAuth:Client");

        return services.AddUltimateAuthClientInternal();
    }

    /// <summary>
    /// Internal shared registration pipeline for UltimateAuth client services.
    ///
    /// This method registers:
    /// - Client infrastructure
    /// - Public client abstractions
    ///
    /// NOTE:
    /// This method does NOT register any server-side services.
    /// </summary>
    private static IServiceCollection AddUltimateAuthClientInternal(this IServiceCollection services)
    {
        services.AddScoped<IUAuthClientProductInfoProvider, UAuthClientProductInfoProvider>();

        services.AddSingleton<IValidateOptions<UAuthClientOptions>, UAuthClientOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthClientOptions>, UAuthClientEndpointOptionsValidator>();

        services.AddSingleton<IClientProfileDetector, UAuthClientProfileDetector>();
        services.AddSingleton<IPostConfigureOptions<UAuthClientOptions>, UAuthClientOptionsPostConfigure>();
        services.TryAddSingleton<IClock, ClientClock>();
        services.AddSingleton<IUAuthClientEvents, UAuthClientEvents>();

        services.PostConfigure<UAuthClientOptions>(o =>
        {
            o.AutoRefresh.Interval ??= TimeSpan.FromMinutes(5);
        });

        services.TryAddScoped<IUAuthClient, UAuthClient>();
        services.TryAddScoped<IFlowClient, UAuthFlowClient>();
        services.TryAddScoped<ISessionClient, UAuthSessionClient>();
        services.TryAddScoped<IUserClient, UAuthUserClient>();
        services.TryAddScoped<IUserIdentifierClient, UAuthUserIdentifierClient>();
        services.TryAddScoped<ICredentialClient, UAuthCredentialClient>();
        services.TryAddScoped<IAuthorizationClient, UAuthAuthorizationClient>();

        
        services.TryAddScoped<IUAuthClientBootstrapper, UAuthClientBootstrapper>();
        services.TryAddScoped<IUAuthStateManager, UAuthStateManager>();
        services.TryAddScoped<IDeviceIdProvider, UAuthDeviceIdProvider>();

        services.AddScoped<IDeviceIdGenerator, UAuthDeviceIdGenerator>();

        services.TryAddScoped<IHubCredentialResolver, NoOpHubCredentialResolver>();
        services.TryAddScoped<IHubCapabilities, NoOpHubCapabilities>();
        services.TryAddScoped<IHubFlowReader, NoOpHubFlowReader>();

        return services;
    }
}
