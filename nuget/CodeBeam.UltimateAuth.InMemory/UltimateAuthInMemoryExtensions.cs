using Microsoft.Extensions.DependencyInjection;
using CodeBeam.UltimateAuth.Credentials.InMemory.Extensions;
using CodeBeam.UltimateAuth.Credentials.Reference.Extensions;
using CodeBeam.UltimateAuth.Users.InMemory.Extensions;
using CodeBeam.UltimateAuth.Users.Reference.Extensions;
using CodeBeam.UltimateAuth.Authorization.InMemory.Extensions;
using CodeBeam.UltimateAuth.Authorization.Reference.Extensions;
using CodeBeam.UltimateAuth.Sessions.InMemory.Extensions;
using CodeBeam.UltimateAuth.Tokens.InMemory.Extensions;
using CodeBeam.UltimateAuth.Authentication.InMemory.Extensions;

namespace CodeBeam.UltimateAuth.InMemory;

/// <summary>
/// Provides extension methods for registering in-memory implementations of UltimateAuth user, credential,
/// authorization, session, token, and authentication services, along with their reference services, in the dependency
/// injection container.
/// </summary>
/// <remarks>These methods are intended for scenarios such as testing or development where in-memory storage is
/// sufficient. For production environments, consider using persistent storage implementations.</remarks>
public static class UltimateAuthInMemoryExtensions
{
    /// <summary>
    /// Registers in-memory implementations of UltimateAuth user, credential, authorization, session, token, and
    /// authentication services, along with their reference services, in the dependency injection container.
    /// </summary>
    /// <remarks>This method is intended for scenarios such as testing or development where in-memory storage
    /// is sufficient. For production environments, consider using persistent storage implementations.</remarks>
    /// <param name="services">The service collection to which the in-memory UltimateAuth services will be added.</param>
    /// <returns>The same instance of <see cref="IServiceCollection"/> that was provided, to support method chaining.</returns>
    public static IServiceCollection AddInMemoryReference(this IServiceCollection services)
    {
        services
            .AddUltimateAuthUsersInMemory()
            .AddUltimateAuthUsersReference()
            .AddUltimateAuthCredentialsInMemory()
            .AddUltimateAuthCredentialsReference()
            .AddUltimateAuthAuthorizationInMemory()
            .AddUltimateAuthAuthorizationReference()
            .AddUltimateAuthSessionsInMemory()
            .AddUltimateAuthTokensInMemory()
            .AddUltimateAuthAuthenticationInMemory();

        return services;
    }
}