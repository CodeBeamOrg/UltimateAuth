using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Integration.EfCore;

public sealed class DeviceLimitPostgresTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static readonly TenantKey Tenant = TenantKey.FromExternal("device-limit-postgres");

    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RevokeOldest_WhenIssuanceFails_LeavesOriginalSessionActive()
    {
        await using var database = await postgres.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(database.ConnectionString);
        var user = UserKey.New();
        var options = Options(maxDevices: 1,
            behavior: DeviceLimitBehavior.RevokeOldest);

        const string duplicatedToken = "opaque-session-token-000000000000000000000001";

        var first = await IssueAsync(
            provider, options, user,
            Device("first-device-00000000000000000001"),
            Now, duplicatedToken);

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await IssueAsync(
                provider, options, user,
                Device("second-device-00000000000000000002"),
                Now.AddMinutes(1), duplicatedToken);
        });

        await using var verification = provider.CreateAsyncScope();
        var store = verification.ServiceProvider
            .GetRequiredService<ISessionStoreFactory>().Create(Tenant);
        var session = await store.GetSessionAsync(
            first.Session.SessionId, TestContext.Current.CancellationToken);
        var chains = await store.GetChainsByUserAsync(
            user, ct: TestContext.Current.CancellationToken);

        Assert.NotNull(session);
        Assert.False(session.IsRevoked);
        Assert.Single(chains);
        Assert.False(chains[0].IsRevoked);
    }

    [Fact]
    public async Task ConcurrentRejectNewLogin_NeverCommitsMoreThanOneDevice()
    {
        await using var database = await postgres.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(database.ConnectionString);
        var user = UserKey.New();
        var options = Options(maxDevices: 1);
        var ct = TestContext.Current.CancellationToken;

        // Seed only the root, leaving one available device slot.
        // Otherwise simultaneous root creation can test a different race.
        await using (var scope = provider.CreateAsyncScope())
        {
            var store = scope.ServiceProvider
                .GetRequiredService<ISessionStoreFactory>().Create(Tenant);
            await store.ExecuteAsync(
                innerCt => store.CreateRootAsync(
                    UAuthSessionRoot.Create(Tenant, user, Now.AddMinutes(-1)),
                    innerCt), ct);
        }

        async Task<Exception?> AttemptAsync(string deviceId)
        {
            try
            {
                await IssueAsync(provider, options, user, Device(deviceId), Now);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        var outcomes = await Task.WhenAll(
            AttemptAsync("first-device-00000000000000000001"),
            AttemptAsync("second-device-00000000000000000002"));

        Assert.Single(outcomes, x => x is null);

        await using var verification = provider.CreateAsyncScope();
        var stored = verification.ServiceProvider
            .GetRequiredService<ISessionStoreFactory>().Create(Tenant);

        var chains = await stored.GetChainsByUserAsync(user, ct: ct);
        Assert.Single(chains, x => !x.IsRevoked);
    }

    [Fact]
    public async Task ConcurrentRevokeOldest_NeverLeavesMultipleActiveDevices()
    {
        await using var database = await postgres.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(database.ConnectionString);
        var user = UserKey.New();
        var options = Options(maxDevices: 1,
            behavior: DeviceLimitBehavior.RevokeOldest);
        var ct = TestContext.Current.CancellationToken;

        await IssueAsync(
            provider, options, user,
            Device("original-device-000000000000000001"),
            Now);

        async Task<Exception?> AttemptAsync(string deviceId)
        {
            try
            {
                await IssueAsync(provider, options, user,
                    Device(deviceId), Now.AddMinutes(1));
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        var outcomes = await Task.WhenAll(
            AttemptAsync("first-device-00000000000000000001"),
            AttemptAsync("second-device-00000000000000000002"));

        // Without an explicit serialization-conflict retry, one of the
        // concurrent transactions may safely fail. Quota integrity is
        // the contract tested here, not unconditional login success.
        Assert.Contains(outcomes, x => x is null);

        await using var verification = provider.CreateAsyncScope();
        var store = verification.ServiceProvider
            .GetRequiredService<ISessionStoreFactory>().Create(Tenant);
        var chains = await store.GetChainsByUserAsync(user, ct: ct);

        Assert.Single(chains, x => !x.IsRevoked);
    }

    private static UAuthServerOptions Options(
        int maxDevices, DeviceLimitBehavior behavior = DeviceLimitBehavior.RejectNewLogin)
    {
        var options = new UAuthServerOptions();
        options.DeviceLimits.MaxDevices = maxDevices;
        options.DeviceLimits.Behavior = behavior;
        return options;
    }

    private static async Task<IssuedSession> IssueAsync(
        IServiceProvider provider,
        UAuthServerOptions options,
        UserKey user,
        DeviceContext device,
        DateTimeOffset now,
        string? fixedToken = null)
    {
        await using var scope = provider.CreateAsyncScope();

        var factory = scope.ServiceProvider
            .GetRequiredService<ISessionStoreFactory>();

        var issuer = new UAuthSessionIssuer(
            factory,
            new TestOpaqueGenerator(fixedToken),
            Microsoft.Extensions.Options.Options.Create(options));

        var context = new SessionIssuanceContext
        {
            Tenant = Tenant,
            UserKey = user,
            Device = device,
            Now = now,
            Claims = ClaimsSnapshot.Empty,
            Metadata = SessionMetadata.Empty,
            Mode = UAuthMode.Hybrid,
            SessionOptions = new UAuthSessionOptions
            {
                Lifetime = TimeSpan.FromHours(8),
                MaxSessionsPerChain = 20
            }
        };

        return await issuer.IssueSessionAsync(
            context, TestContext.Current.CancellationToken);
    }

    private static DeviceContext Device(string id) =>
        DeviceContext.Create(
            DeviceId.Create(id),
            deviceType: "web",
            platform: "web",
            operatingSystem: "test-os",
            browser: "test-browser",
            ipAddress: "127.0.0.1");

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<UAuthDbContext>(
            options => options.UseNpgsql(connectionString));

        services.AddUltimateAuthSessionsEntityFrameworkCore<UAuthDbContext>();

        var provider = services.BuildServiceProvider(validateScopes: true);
        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<UAuthDbContext>();
        await db.Database.EnsureCreatedAsync(
            TestContext.Current.CancellationToken);

        return provider;
    }

    private sealed class TestOpaqueGenerator(string? fixedToken) : IOpaqueTokenGenerator
    {
        public string Generate() =>
            fixedToken ?? "opaque-session-token-" + Guid.NewGuid().ToString("N");

        public string GenerateJwtId() => Guid.NewGuid().ToString("N");
    }
}
