using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Blazor.Device;
using CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;
using CodeBeam.UltimateAuth.Client.Device;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Client.Blazor.Extensions;

/// <summary>
/// Provides extension methods for registering UltimateAuth client blazor services.
/// This layer method included core client capabilities with blazor components and storage.
/// This extension can safely be used together with AddUltimateAuthServer() in Blazor Server applications.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers UltimateAuth client services with Blazor adapter services.
    /// This package depends on CodeBeam.UltimateAuth.Client and completes it by supplying platform-specific implementations.
    /// This ensures that all required abstractions from the core client are properly wired for Blazor applications.
    /// So, do not use "AddUltimateAuthClient" when "AddUltimateAuthClientBlazor" already specified.
    /// </summary>
    public static IServiceCollection AddUltimateAuthClientBlazor(this IServiceCollection services, Action<UAuthClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddUltimateAuthClient(configure);

        return services.AddUltimateAuthClientBlazorInternal();
    }

    /// <summary>
    /// Internal shared registration pipeline for UltimateAuth client services.
    /// This method does NOT register any server-side services.
    /// </summary>
    private static IServiceCollection AddUltimateAuthClientBlazorInternal(this IServiceCollection services)
    {
        services.TryAddScoped<IUAuthRequestClient, UAuthRequestClient>();

        services.TryAddScoped<ISessionCoordinator, SessionCoordinator>();
        services.AddScoped<UAuthClientDiagnostics>();

        services.AddScoped<AuthenticationStateProvider, UAuthAuthenticationStateProvider>();
        services.TryAddScoped<IClientStorage, BrowserClientStorage>();
        services.TryAddScoped<IDeviceIdStorage, BrowserDeviceIdStorage>();

        services.AddScoped<IBrowserUAuthBridge, BrowserUAuthBridge>();
        services.AddScoped<IReturnUrlProvider, BlazorReturnUrlProvider>();

        services.AddAuthorizationCore();

        return services;
    }
}
