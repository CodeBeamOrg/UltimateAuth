using CodeBeam.UltimateAuth.Authentication.InMemory;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Sample.Seed.Extensions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal sealed class TestAuthRuntime<TUserId> where TUserId : notnull
{
    public IServiceProvider Services { get; }
    public TestClock Clock { get; }

    public TestAuthRuntime(Action<UAuthServerOptions>? configureServer = null, Action<UAuthOptions>? configureCore = null)
    {
        Clock = new TestClock();
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddUltimateAuth(configureCore ?? (_ => { }));
        services.AddUltimateAuthServer(options =>
        {
            configureServer?.Invoke(options);
        });

        services.AddUltimateAuthSampleSeed();

        services.AddSingleton<IUAuthPasswordHasher, TestPasswordHasher>();
        // InMemory plugins
        services.AddUltimateAuthInMemory();


        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IClock>(Clock);

        Services = services.BuildServiceProvider();

        using (var scope = Services.CreateScope())
        {
            var seedRunner = scope.ServiceProvider.GetRequiredService<SeedRunner>();
            seedRunner.RunAsync(null).GetAwaiter().GetResult();
        }

        //Services = services.BuildServiceProvider();
        //Services.GetRequiredService<SeedRunner>().RunAsync(null).GetAwaiter().GetResult();
    }

    public ILoginOrchestrator GetLoginOrchestrator()
        => Services.GetRequiredService<ILoginOrchestrator>();

    public ValueTask<AuthFlowContext> CreateLoginFlowAsync(TenantKey? tenant = null)
    {
        var httpContext = TestHttpContext.Create(tenant);
        return Services.GetRequiredService<IAuthFlowContextFactory>().CreateAsync(httpContext, AuthFlowType.Login);
    }

    public async Task<LoginResult> LoginAsync(AuthFlowContext flow, AuthExecutionContext execution, LoginRequest request, CancellationToken ct = default)
    {
        using var scope = Services.CreateScope();
        var flowService = scope.ServiceProvider.GetRequiredService<IUAuthFlowService>();

        return await flowService.LoginAsync(flow, execution, request, ct);
    }

    public async Task<TestLoginUser> CreateLoginUserAsync(string? identifier = null, string? secret = null, CancellationToken ct = default)
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

        var tenant = TenantKeys.Single;
        var userKey = UserKey.New();

        identifier ??= $"unit-{Guid.NewGuid():N}";
        secret ??= $"Test-{Guid.NewGuid():N}!";

        var now = Clock.UtcNow;

        var lifecycleStore = lifecycleFactory.Create(tenant);
        var identifierStore = identifierFactory.Create(tenant);
        var credentialStore = credentialFactory.Create(tenant);

        await lifecycleStore.AddAsync(
            UserLifecycle.Create(
                tenant,
                userKey,
                now),
            ct);

        var normalized = normalizer.Normalize(
            UserIdentifierType.Username,
            identifier).Normalized;

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

        return new TestLoginUser(userKey, identifier, secret);
    }

    public IUserApplicationService GetUserApplicationService()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IUserApplicationService>();
    }

    public ICredentialManagementService GetCredentialManagementService()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ICredentialManagementService>();
    }

    public async Task<LoginResult> LoginAsync(AuthFlowContext flow)
    {
        using var scope = Services.CreateScope();

        var orchestrator = scope.ServiceProvider
            .GetRequiredService<ILoginOrchestrator>();

        return await orchestrator.LoginAsync(flow, new LoginRequest
        {
            Identifier = "user",
            Secret = "user"
        });
    }

    public async Task<UserIdentifier> AddIdentifierAsync(UserKey userKey, UserIdentifierType type, string value, TenantKey? tenant = null,
                                      bool isPrimary = true, bool isVerified = true, CancellationToken ct = default)
    {
        using var scope = Services.CreateScope();
        var services = scope.ServiceProvider;
        var identifierFactory = services.GetRequiredService<IUserIdentifierStoreFactory>();
        var normalizer = services.GetRequiredService<IIdentifierNormalizer>();

        var effectiveTenant = tenant ?? TenantKeys.Single;
        var now = Clock.UtcNow;

        var normalized = normalizer.Normalize(type, value);

        if (!normalized.IsValid)
            throw new InvalidOperationException($"Test identifier could not be normalized: {normalized.ErrorCode}");

        var identifier = UserIdentifier.Create(
            id: Guid.NewGuid(),
            tenant: effectiveTenant,
            userKey: userKey,
            type: type,
            value: value,
            normalizedValue: normalized.Normalized,
            now: now,
            isPrimary: isPrimary,
            verifiedAt: isVerified ? now : null);

        var store = identifierFactory.Create(effectiveTenant);
        await store.AddAsync(identifier, ct);

        return identifier;
    }

    internal sealed record TestLoginUser(UserKey UserKey, string Identifier, string Secret);
}
