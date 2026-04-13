using CodeBeam.UltimateAuth.Authorization.Reference.Extensions;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Credentials.Reference.Extensions;
using CodeBeam.UltimateAuth.Security.Argon2;
using CodeBeam.UltimateAuth.Users.Reference.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Reference.Bundle;

public static class UltimateAuthReferenceBundleExtensions
{
    /// <summary>
    /// Registers all UltimateAuth reference implementations in a single step.
    /// </summary>
    /// <remarks>
    /// This method adds the default reference behavior for:
    /// <list type="bullet">
    /// <item><description>Users</description></item>
    /// <item><description>Credentials</description></item>
    /// <item><description>Authorization</description></item>
    /// </list>
    ///
    /// It is intended as a convenience bundle for quickly enabling a fully working
    /// domain layer on top of <c>UltimateAuth Server</c>.
    ///
    /// <para>
    /// This package does not provide persistence. To complete the setup, you must also
    /// register a persistence provider such as:
    /// </para>
    /// <list type="bullet">
    /// <item><description>InMemory</description></item>
    /// <item><description>EntityFrameworkCore</description></item>
    /// </list>
    ///
    /// <para>
    /// Advanced users can skip this bundle and register individual reference packages for finer control.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddUltimateAuthReferences(this IServiceCollection services)
    {
        services
            .AddUltimateAuthUsersReference()
            .AddUltimateAuthCredentialsReference()
            .AddUltimateAuthAuthorizationReference();

        services.TryAddSingleton<IUAuthPasswordHasher, Argon2PasswordHasher>();
        services.AddOptions<Argon2Options>();

        return services;
    }
}
