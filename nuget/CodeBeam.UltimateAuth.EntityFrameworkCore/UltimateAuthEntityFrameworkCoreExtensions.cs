using CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.Authorization.Reference.Extensions;
using CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.Credentials.Reference.Extensions;
using CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.Users.EntityFrameworkCore.Extensions;
using CodeBeam.UltimateAuth.Users.Reference.Extensions;
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
    /// This method wires up all Entity Framework Core stores along with reference domain implementations.
    /// 
    /// Example:
    /// <code>
    /// services.AddUltimateAuthServer()
    ///     .AddEntityFrameworkReference(options =>
    ///     {
    ///         options.UseSqlServer("connection-string");
    ///     });
    /// </code>
    /// 
    /// Note:
    /// This method does not configure migrations automatically. You are responsible for managing migrations.
    /// </remarks>
    public static IServiceCollection AddEntityFrameworkReference(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services
            .AddUltimateAuthUsersEntityFrameworkCore(configureDb)
            .AddUltimateAuthUsersReference()
            .AddUltimateAuthCredentialsEntityFrameworkCore(configureDb)
            .AddUltimateAuthCredentialsReference()
            .AddUltimateAuthAuthorizationEntityFrameworkCore(configureDb)
            .AddUltimateAuthAuthorizationReference()
            .AddUltimateAuthSessionsEntityFrameworkCore(configureDb)
            .AddUltimateAuthTokensEntityFrameworkCore(configureDb)
            .AddUltimateAuthAuthenticationEntityFrameworkCore(configureDb);

        return services;
    }

    /// <summary>
    /// Adds and configures Entity Framework Core-based UltimateAuth services and related references to the specified
    /// service collection.
    /// </summary>
    /// <remarks>This method registers all required UltimateAuth services for users, credentials,
    /// authorization, sessions, tokens, and authentication using Entity Framework Core. It should be called during
    /// application startup as part of service configuration.</remarks>
    /// <param name="services">The service collection to which the UltimateAuth Entity Framework Core services and references will be added.</param>
    /// <param name="configure">A delegate that configures the options for UltimateAuth Entity Framework Core integration.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddEntityFrameworkReference(this IServiceCollection services, Action<UAuthEfCoreOptions> configure)
    {
        var options = new UAuthEfCoreOptions();
        configure(options);

        services
        .AddUltimateAuthUsersEntityFrameworkCore(options.Resolve(options.Users))
        .AddUltimateAuthUsersReference()
        .AddUltimateAuthCredentialsEntityFrameworkCore(options.Resolve(options.Credentials))
        .AddUltimateAuthCredentialsReference()
        .AddUltimateAuthAuthorizationEntityFrameworkCore(options.Resolve(options.Authorization))
        .AddUltimateAuthAuthorizationReference()
        .AddUltimateAuthSessionsEntityFrameworkCore(options.Resolve(options.Sessions))
        .AddUltimateAuthTokensEntityFrameworkCore(options.Resolve(options.Tokens))
        .AddUltimateAuthAuthenticationEntityFrameworkCore(options.Resolve(options.Authentication));

        return services;
    }
}
