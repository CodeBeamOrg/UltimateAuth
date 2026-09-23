using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Sessions.Contracts;

public abstract class SessionStoreChainContractTests
{
    protected abstract Task<ISessionStoreTestDatabase> CreateDatabaseAsync();

    protected static readonly TenantKey Tenant =
        TenantKey.FromExternal("tenant-a");

    protected static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    protected static async Task<(
        UAuthSessionRoot Root,
        UAuthSessionChain Chain)>
        CreateChainGraphAsync(
            ISessionStore store,
            TenantKey tenant,
            UserKey? userKey = null)
    {
        var user = userKey ?? UserKey.New();

        var root = UAuthSessionRoot.Create(
            tenant,
            user,
            Now);

        await store.ExecuteAsync(
            ct => store.CreateRootAsync(root, ct));

        var chain = UAuthSessionChain.Create(
            SessionChainId.New(),
            root.RootId,
            tenant,
            user,
            Now,
            Now.AddDays(30),
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            root.SecurityVersion);

        await store.ExecuteAsync(
            ct => store.CreateChainAsync(chain, ct));

        return (root, chain);
    }

    private static UAuthSessionChain CreateChain(
        UAuthSessionRoot root,
        TenantKey tenant,
        UserKey user)
    {
        return UAuthSessionChain.Create(
            SessionChainId.New(),
            root.RootId,
            tenant,
            user,
            Now,
            Now.AddDays(30),
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            root.SecurityVersion);
    }

    // ------------------------------------------------------------
    // CREATE / GET
    // ------------------------------------------------------------

    [Fact]
    public async Task CreateChainAsync_PersistsChain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (root, chain) =
            await CreateChainGraphAsync(store, Tenant);

        var result =
            await store.GetChainAsync(chain.ChainId);

        result.Should().NotBeNull();

        result!.ChainId.Should().Be(chain.ChainId);
        result.RootId.Should().Be(root.RootId);
        result.Tenant.Should().Be(Tenant);
        result.UserKey.Should().Be(chain.UserKey);
        result.CreatedAt.Should().Be(Now);
        result.SecurityVersionAtCreation
            .Should().Be(root.SecurityVersion);
        result.ActiveSessionId.Should().BeNull();
        result.IsRevoked.Should().BeFalse();
        result.Version.Should().Be(0);
    }

    [Fact]
    public async Task GetChainAsync_WhenMissing_ReturnsNull()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var result =
            await store.GetChainAsync(
                SessionChainId.New());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateChainAsync_WhenVersionIsNotZero_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var root = UAuthSessionRoot.Create(
            Tenant,
            user,
            Now);

        await store.ExecuteAsync(
            ct => store.CreateRootAsync(root, ct));

        var chain = CreateChain(
            root,
            Tenant,
            user);

        chain.Version = 1;

        var act = () => store.ExecuteAsync(
            ct => store.CreateChainAsync(chain, ct));

        await act.Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateChainAsync_WhenIdAlreadyExists_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (root, first) =
            await CreateChainGraphAsync(
                store,
                Tenant);

        var second = UAuthSessionChain.Create(
            first.ChainId,
            root.RootId,
            Tenant,
            first.UserKey,
            Now.AddMinutes(1),
            Now.AddDays(30),
            TestDevice.Default(),
            ClaimsSnapshot.Empty,
            root.SecurityVersion);

        var act = () => store.ExecuteAsync(
            ct => store.CreateChainAsync(
                second,
                ct));

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    // ------------------------------------------------------------
    // SAVE
    // ------------------------------------------------------------

    [Fact]
    public async Task SaveChainAsync_WhenExpectedVersionMatches_PersistsChanges()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (_, chain) =
            await CreateChainGraphAsync(
                store,
                Tenant);

        var updated =
            chain.Touch(
                Now.AddMinutes(10));

        await store.ExecuteAsync(
            ct => store.SaveChainAsync(
                updated,
                expectedVersion: chain.Version,
                ct));

        var result =
            await store.GetChainAsync(
                chain.ChainId);

        result.Should().NotBeNull();
        result!.LastSeenAt.Should().Be(
            Now.AddMinutes(10));
        result.TouchCount.Should().Be(1);
        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveChainAsync_WhenMissing_ThrowsNotFound()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var root = UAuthSessionRoot.Create(
            Tenant,
            user,
            Now);

        var chain = CreateChain(
            root,
            Tenant,
            user);

        var updated =
            chain.Touch(Now.AddMinutes(10));

        var act = () => store.ExecuteAsync(
            ct => store.SaveChainAsync(
                updated,
                expectedVersion: chain.Version,
                ct));

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();
    }

    [Fact]
    public async Task SaveChainAsync_WhenExpectedVersionIsStale_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (_, chain) =
            await CreateChainGraphAsync(
                store,
                Tenant);

        var updated =
            chain.Touch(Now.AddMinutes(10));

        var act = () => store.ExecuteAsync(
            ct => store.SaveChainAsync(
                updated,
                expectedVersion: 999,
                ct));

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    // ------------------------------------------------------------
    // REVOKE
    // ------------------------------------------------------------

    [Fact]
    public async Task RevokeChainAsync_RevokesChain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (_, chain) =
            await CreateChainGraphAsync(
                store,
                Tenant);

        await store.ExecuteAsync(
            ct => store.RevokeChainAsync(
                chain.ChainId,
                Now.AddMinutes(10),
                ct));

        var result =
            await store.GetChainAsync(
                chain.ChainId);

        result.Should().NotBeNull();
        result!.IsRevoked.Should().BeTrue();
        result.RevokedAt.Should().Be(
            Now.AddMinutes(10));
        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task RevokeChainAsync_WhenMissing_IsIdempotent()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var act = () => store.ExecuteAsync(
            ct => store.RevokeChainAsync(
                SessionChainId.New(),
                Now,
                ct));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeChainAsync_WhenAlreadyRevoked_DoesNotChangeVersionAgain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var (_, chain) =
            await CreateChainGraphAsync(
                store,
                Tenant);

        await store.ExecuteAsync(
            ct => store.RevokeChainAsync(
                chain.ChainId,
                Now.AddMinutes(5),
                ct));

        await store.ExecuteAsync(
            ct => store.RevokeChainAsync(
                chain.ChainId,
                Now.AddMinutes(10),
                ct));

        var result =
            await store.GetChainAsync(
                chain.ChainId);

        result.Should().NotBeNull();
        result!.RevokedAt.Should().Be(
            Now.AddMinutes(5));
        result.Version.Should().Be(1);
    }

    // ------------------------------------------------------------
    // QUERY
    // ------------------------------------------------------------

    [Fact]
    public async Task GetChainsByRootAsync_ReturnsOnlyRequestedRoot()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var userA = UserKey.New();
        var userB = UserKey.New();

        var (rootA, chainA) =
            await CreateChainGraphAsync(
                store,
                Tenant,
                userA);

        var secondChainA =
            CreateChain(
                rootA,
                Tenant,
                userA);

        await store.ExecuteAsync(
            ct => store.CreateChainAsync(
                secondChainA,
                ct));

        var (_, chainB) =
            await CreateChainGraphAsync(
                store,
                Tenant,
                userB);

        var result =
            await store.GetChainsByRootAsync(
                rootA.RootId);

        result.Should().HaveCount(2);

        result.Select(x => x.ChainId)
            .Should()
            .BeEquivalentTo([
                chainA.ChainId,
                secondChainA.ChainId
            ]);

        result.Should()
            .NotContain(x =>
                x.ChainId == chainB.ChainId);
    }

    [Fact]
    public async Task GetChainsByUserAsync_ReturnsUsersChains()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var (root, first) =
            await CreateChainGraphAsync(
                store,
                Tenant,
                user);

        var second =
            CreateChain(
                root,
                Tenant,
                user);

        await store.ExecuteAsync(
            ct => store.CreateChainAsync(
                second,
                ct));

        var result =
            await store.GetChainsByUserAsync(user);

        result.Select(x => x.ChainId)
            .Should()
            .BeEquivalentTo([
                first.ChainId,
                second.ChainId
            ]);
    }

    // ------------------------------------------------------------
    // TENANT
    // ------------------------------------------------------------

    [Fact]
    public async Task GetChainAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA =
            TenantKey.FromExternal("tenant-a");

        var tenantB =
            TenantKey.FromExternal("tenant-b");

        var storeA = db.CreateStore(tenantA);
        var storeB = db.CreateStore(tenantB);

        var (_, chain) =
            await CreateChainGraphAsync(
                storeA,
                tenantA);

        var fromA =
            await storeA.GetChainAsync(
                chain.ChainId);

        var fromB =
            await storeB.GetChainAsync(
                chain.ChainId);

        fromA.Should().NotBeNull();
        fromB.Should().BeNull();
    }

    [Fact]
    public async Task CreateChainAsync_WhenChainBelongsToDifferentTenant_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA =
            TenantKey.FromExternal("tenant-a");

        var tenantB =
            TenantKey.FromExternal("tenant-b");

        var storeA = db.CreateStore(tenantA);

        var rootB = UAuthSessionRoot.Create(
            tenantB,
            UserKey.New(),
            Now);

        var chainB = CreateChain(
            rootB,
            tenantB,
            rootB.UserKey);

        var act = () => storeA.ExecuteAsync(
            ct => storeA.CreateChainAsync(
                chainB,
                ct));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Tenant mismatch.");
    }

    // ------------------------------------------------------------
    // CANCELLATION
    // ------------------------------------------------------------

    [Fact]
    public async Task GetChainAsync_WhenCancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        using var cts =
            new CancellationTokenSource();

        cts.Cancel();

        var act = () =>
            store.GetChainAsync(
                SessionChainId.New(),
                cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }
}
