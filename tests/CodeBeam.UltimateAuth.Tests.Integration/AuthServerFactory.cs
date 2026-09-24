using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public class AuthServerFactory : WebApplicationFactory<Program>
{
    public IntegrationTestClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);
        });
    }

    internal async Task<IntegrationTestUser> CreateLoginUserAsync(
        string? identifier = null,
        string? secret = null,
        CancellationToken ct = default)
    {
        using var scope = Services.CreateScope();

        var services = scope.ServiceProvider;

        var lifecycleFactory =
            services.GetRequiredService<IUserLifecycleStoreFactory>();

        var identifierFactory =
            services.GetRequiredService<IUserIdentifierStoreFactory>();

        var credentialFactory =
            services.GetRequiredService<IPasswordCredentialStoreFactory>();

        var normalizer =
            services.GetRequiredService<IIdentifierNormalizer>();

        var hasher =
            services.GetRequiredService<IUAuthPasswordHasher>();

        var clock =
            services.GetRequiredService<IClock>();

        var tenant = TenantKeys.Single;
        var userKey = UserKey.New();

        identifier ??= $"test-{Guid.NewGuid():N}";
        secret ??= $"Test-{Guid.NewGuid():N}!";

        var now = clock.UtcNow;

        var lifecycleStore = lifecycleFactory.Create(tenant);
        var identifierStore = identifierFactory.Create(tenant);
        var credentialStore = credentialFactory.Create(tenant);

        await lifecycleStore.AddAsync(
            UserLifecycle.Create(
                tenant,
                userKey,
                now),
            ct);

        var normalized = normalizer
            .Normalize(
                UserIdentifierType.Username,
                identifier)
            .Normalized;

        await identifierStore.AddAsync(
            UserIdentifier.Create(
                Guid.NewGuid(),
                tenant,
                userKey,
                UserIdentifierType.Username,
                identifier,
                normalized,
                now,
                isPrimary: true,
                verifiedAt: now),
            ct);

        await credentialStore.AddAsync(
            PasswordCredential.Create(
                Guid.NewGuid(),
                tenant,
                userKey,
                hasher.Hash(secret),
                CredentialSecurityState.Active(),
                new CredentialMetadata(),
                now),
            ct);

        return new IntegrationTestUser(
            userKey,
            identifier,
            secret);
    }
}
