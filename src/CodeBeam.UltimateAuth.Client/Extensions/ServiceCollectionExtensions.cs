using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Authentication;
using CodeBeam.UltimateAuth.Client.Device;
using CodeBeam.UltimateAuth.Client.Devices;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Runtime;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Components.Authorization;
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
    public static IServiceCollection AddUltimateAuthClient(this IServiceCollection services, Action<UAuthClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<UAuthClientOptions>()
            // Program.cs configuration (lowest precedence)
            .Configure(options =>
            {
                configure?.Invoke(options);
            })
            // appsettings.json (highest precedence)
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

        services.AddOptions<UAuthClientOptions>();
        services.AddSingleton<IValidateOptions<UAuthClientOptions>, UAuthClientOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthClientOptions>, UAuthClientEndpointOptionsValidator>();

        services.AddSingleton<IClientProfileDetector, UAuthClientProfileDetector>();
        services.AddSingleton<IPostConfigureOptions<UAuthClientOptions>, UAuthClientOptionsPostConfigure>();
        services.TryAddSingleton<IClock, ClientClock>();

        services.PostConfigure<UAuthClientOptions>(o =>
        {
            o.AutoRefresh.Interval ??= TimeSpan.FromMinutes(5);
        });

        services.TryAddScoped<IUAuthRequestClient, UAuthRequestClient>();
        services.TryAddScoped<IUAuthClient, UAuthClient>();
        services.TryAddScoped<IFlowClient, UAuthFlowClient>();
        services.TryAddScoped<IUserClient, UAuthUserClient>();
        services.TryAddScoped<IUserIdentifierClient, UAuthUserIdentifierClient>();
        services.TryAddScoped<ICredentialClient, UAuthCredentialClient>();
        services.TryAddScoped<IAuthorizationClient, UAuthAuthorizationClient>();

        services.TryAddScoped<ISessionCoordinator, SessionCoordinator>();
        services.TryAddScoped<IUAuthClientBootstrapper, UAuthClientBootstrapper>();
        services.AddScoped<UAuthClientDiagnostics>();
        services.TryAddScoped<IUAuthStateManager, UAuthStateManager>();
        services.AddScoped<AuthenticationStateProvider, UAuthAuthenticationStateProvider>();
        services.TryAddScoped<IDeviceIdProvider, UAuthDeviceIdProvider>();
        services.TryAddScoped<IBrowserStorage, BrowserStorage>();
        services.TryAddScoped<IDeviceIdStorage, BrowserDeviceIdStorage>();

        services.AddScoped<IDeviceIdGenerator, UAuthDeviceIdGenerator>();
        services.AddScoped<IBrowserUAuthBridge, BrowserUAuthBridge>();

        services.TryAddScoped<IHubCredentialResolver, NoOpHubCredentialResolver>();
        services.TryAddScoped<IHubCapabilities, NoOpHubCapabilities>();
        services.TryAddScoped<IHubFlowReader, NoOpHubFlowReader>();

        //services.AddScoped<UAuthCascadingStateProvider>();
        //services.AddScoped<CascadingValueSource<UAuthState>>(sp => sp.GetRequiredService<UAuthCascadingStateProvider>());

        return services;
    }
}
