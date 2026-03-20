using CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.Reference.Bundle;
using CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.Users.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore;

/// <summary>
/// Provides extension methods for registering UltimateAuth with Entity Framework Core-based persistence using reference
/// domain implementations.
/// </summary>
public static class UltimateAuthEntityFrameworkCoreExtensions
{
    /// <summary>
    /// Registers UltimateAuth with Entity Framework Core based persistence using reference domain implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDb">
    /// A delegate used to configure the <see cref="DbContextOptionsBuilder"/> for all UltimateAuth DbContexts.
    /// 
    /// This is required and must specify a database provider such as:
    /// <list type="bullet">
    /// <item><description>UseSqlServer</description></item>
    /// <item><description>UseNpgsql</description></item>
    /// <item><description>UseSqlite</description></item>
    /// </list>
    /// </param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    /// <remarks>
    /// This method provides a complete UltimateAuth setup including:
    /// <list type="bullet">
    /// <item><description>Reference domain implementations</description></item>
    /// <item><description>Entity Framework Core persistence</description></item>
    /// </list>
    ///
    /// No additional reference packages are required.
    /// 
    /// This method wires up all Entity Framework Core stores along with reference domain implementations.
    /// 
    /// Example:
    /// <code>
    /// services.AddUltimateAuthServer()
    ///     .AddUltimateAuthEntityFrameworkCore(options =>
    ///     {
    ///         options.UseSqlServer("connection-string");
    ///     });
    /// </code>
    /// 
    /// Note:
    /// This method does not configure migrations automatically. You are responsible for managing migrations.
    /// </remarks>
    public static IServiceCollection AddUltimateAuthEntityFrameworkCore(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services
            .AddUltimateAuthReferences()
            .AddUltimateAuthUsersEntityFrameworkCore(configureDb)
            .AddUltimateAuthCredentialsEntityFrameworkCore(configureDb)
            .AddUltimateAuthAuthorizationEntityFrameworkCore(configureDb)
            .AddUltimateAuthSessionsEntityFrameworkCore(configureDb)
            .AddUltimateAuthTokensEntityFrameworkCore(configureDb)
            .AddUltimateAuthAuthenticationEntityFrameworkCore(configureDb);

        return services;
    }

    /// <summary>
    /// Adds and configures Entity Framework Core-based UltimateAuth services and related references to the specified
    /// service collection.
    /// </summary>
    /// <remarks>
    /// This method provides a complete UltimateAuth setup including:
    /// <list type="bullet">
    /// <item><description>Reference domain implementations</description></item>
    /// <item><description>Entity Framework Core persistence</description></item>
    /// </list>
    ///
    /// No additional reference packages are required.
    /// </remarks>
    /// <param name="services">The service collection to which the UltimateAuth Entity Framework Core services and references will be added.</param>
    /// <param name="configure">A delegate that configures the options for UltimateAuth Entity Framework Core integration.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddUltimateAuthEntityFrameworkCore(this IServiceCollection services, Action<UAuthEfCoreOptions> configure)
    {
        var options = new UAuthEfCoreOptions();
        configure(options);

        services
            .AddUltimateAuthReferences()
            .AddUltimateAuthUsersEntityFrameworkCore(options.Resolve(options.Users))
            .AddUltimateAuthCredentialsEntityFrameworkCore(options.Resolve(options.Credentials))
            .AddUltimateAuthAuthorizationEntityFrameworkCore(options.Resolve(options.Authorization))
            .AddUltimateAuthSessionsEntityFrameworkCore(options.Resolve(options.Sessions))
            .AddUltimateAuthTokensEntityFrameworkCore(options.Resolve(options.Tokens))
            .AddUltimateAuthAuthenticationEntityFrameworkCore(options.Resolve(options.Authentication));

        return services;
    }
}
