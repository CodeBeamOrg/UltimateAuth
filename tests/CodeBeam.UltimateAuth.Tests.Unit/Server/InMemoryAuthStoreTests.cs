using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Stores;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class InMemoryAuthStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CleanupExpired_RemovesUnreadExpiredArtifacts()
    {
        var clock = new TestClock(Now);
        var store = CreateStore(clock);

        var key = new AuthArtifactKey("expired");

        await store.StoreAsync(
            key, CreateArtifact(Now.AddMinutes(1)), TestContext.Current.CancellationToken);

        Assert.Equal(1, store.Count);

        clock.UtcNow = Now.AddMinutes(2);

        // No GetAsync or ConsumeAsync has been called.
        var removed = store.CleanupExpired();

        Assert.Equal(1, removed);
        Assert.Equal(0, store.Count);
        Assert.Null(await store.GetAsync(key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CleanupExpired_PreservesUnexpiredArtifacts()
    {
        var clock = new TestClock(Now);
        var store = CreateStore(clock);

        var key = new AuthArtifactKey("valid");

        await store.StoreAsync(
            key, CreateArtifact(Now.AddMinutes(5)), TestContext.Current.CancellationToken);

        clock.UtcNow = Now.AddMinutes(2);

        Assert.Equal(0, store.CleanupExpired());
        Assert.Equal(1, store.Count);

        var consumed = await store.ConsumeAsync(key, TestContext.Current.CancellationToken);

        Assert.NotNull(consumed);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task StoreAsync_WhenFull_RejectsWithoutEvictingValidProof()
    {
        var clock = new TestClock(Now);
        var store = CreateStore(clock, maxEntries: 1);

        var existingKey = new AuthArtifactKey("existing");

        await store.StoreAsync(
            existingKey, CreateArtifact(Now.AddMinutes(5)), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UAuthArtifactStoreFullException>(
            () => store.StoreAsync(
                new AuthArtifactKey("additional"),
                CreateArtifact(Now.AddMinutes(5)), TestContext.Current.CancellationToken));

        Assert.Equal(1, store.Count);
        Assert.NotNull(await store.ConsumeAsync(existingKey, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StoreAsync_WhenFull_ReclaimsExpiredCapacity()
    {
        var clock = new TestClock(Now);
        var store = CreateStore(clock, maxEntries: 1);

        await store.StoreAsync(
            new AuthArtifactKey("old"),
            CreateArtifact(Now.AddMinutes(1)), TestContext.Current.CancellationToken);

        clock.UtcNow = Now.AddMinutes(2);

        var newKey = new AuthArtifactKey("new");

        // Must reclaim expired entries before rejecting a new proof.
        await store.StoreAsync(
            newKey, CreateArtifact(Now.AddMinutes(5)), TestContext.Current.CancellationToken);

        Assert.Equal(1, store.Count);
        Assert.NotNull(await store.ConsumeAsync(newKey, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConsumeAsync_ConcurrentCalls_OnlyOneSucceeds()
    {
        var clock = new TestClock(Now);
        var store = CreateStore(clock);

        var key = new AuthArtifactKey("consume-once");

        await store.StoreAsync(
            key, CreateArtifact(Now.AddMinutes(5)), TestContext.Current.CancellationToken);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 32)
                .Select(_ => Task.Run(
                    async () => await store.ConsumeAsync(key, TestContext.Current.CancellationToken))));

        Assert.Single(results, artifact => artifact is not null);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task StoreAsync_ConcurrentWrites_NeverExceedsCapacity()
    {
        var clock = new TestClock(Now);
        const int capacity = 10;

        var store = CreateStore(clock, capacity);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 100).Select(i => Task.Run(async () =>
            {
                try
                {
                    await store.StoreAsync(
                        new AuthArtifactKey($"artifact-{i}"),
                        CreateArtifact(Now.AddMinutes(5)));

                    return true;
                }
                catch (UAuthArtifactStoreFullException)
                {
                    return false;
                }
            })));

        Assert.Equal(capacity, results.Count(success => success));
        Assert.Equal(capacity, store.Count);
    }

    private static InMemoryAuthStore CreateStore(
        TestClock clock,
        int maxEntries = 100)
    {
        return new InMemoryAuthStore(
            clock,
            Options.Create(new UAuthServerOptions
            {
                AuthArtifacts = new UAuthArtifactStoreOptions
                {
                    MaxEntries = maxEntries,
                    CleanupInterval = TimeSpan.FromMinutes(1)
                }
            }));
    }

    private static LoginPreviewArtifact CreateArtifact(
        DateTimeOffset expiresAt)
    {
        var flow = AuthFlowTestFactory.LoginSuccess();

        return new LoginPreviewArtifact(
            flow.Tenant,
            UserKey.New(),
            CredentialType.Password,
            flow.Device.DeviceId!.Value.Value,
            "alice",
            flow.ClientProfile,
            "test-fingerprint",
            expiresAt);
    }

    private sealed class TestClock(DateTimeOffset now) : IClock
    {
        private long _utcTicks = now.UtcDateTime.Ticks;

        public DateTimeOffset UtcNow
        {
            get => new(
                Interlocked.Read(ref _utcTicks),
                TimeSpan.Zero);

            set => Interlocked.Exchange(
                ref _utcTicks,
                value.UtcDateTime.Ticks);
        }
    }
}
