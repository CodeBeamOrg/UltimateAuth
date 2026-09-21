using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Sessions.Contracts;

public abstract class SessionStoreRootContractTests
{
    protected abstract Task<ISessionStoreTestDatabase> CreateDatabaseAsync();

    protected static readonly TenantKey Tenant =
        TenantKey.FromExternal("tenant-a");

    protected static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static async Task<UAuthSessionRoot> CreateRootAsync(
        ISessionStore store,
        TenantKey tenant,
        UserKey? userKey = null)
    {
        var root = UAuthSessionRoot.Create(
            tenant,
            userKey ?? UserKey.New(),
            Now);

        await store.ExecuteAsync(
            ct => store.CreateRootAsync(root, ct));

        return root;
    }

    // ------------------------------------------------------------
    // CREATE / GET
    // ------------------------------------------------------------

    [Fact]
    public async Task CreateRootAsync_PersistsRoot()
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

        var result =
            await store.GetRootByUserAsync(user);

        result.Should().NotBeNull();

        result!.RootId.Should().Be(root.RootId);
        result.Tenant.Should().Be(Tenant);
        result.UserKey.Should().Be(user);

        result.CreatedAt.Should().Be(Now);

        // new root not mutated yet
        result.UpdatedAt.Should().BeNull();

        result.RevokedAt.Should().BeNull();
        result.IsRevoked.Should().BeFalse();

        result.SecurityVersion.Should().Be(0);
        result.Version.Should().Be(0);
    }

    [Fact]
    public async Task GetRootByUserAsync_WhenMissing_ReturnsNull()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var result =
            await store.GetRootByUserAsync(
                UserKey.New());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRootByIdAsync_ReturnsRoot()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var root =
            await CreateRootAsync(
                store,
                Tenant);

        var result =
            await store.GetRootByIdAsync(
                root.RootId);

        result.Should().NotBeNull();

        result!.RootId.Should().Be(root.RootId);
        result.UserKey.Should().Be(root.UserKey);
        result.Tenant.Should().Be(Tenant);
    }

    [Fact]
    public async Task GetRootByIdAsync_WhenMissing_ReturnsNull()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var missingRoot =
            UAuthSessionRoot.Create(
                Tenant,
                UserKey.New(),
                Now);

        var result =
            await store.GetRootByIdAsync(
                missingRoot.RootId);

        result.Should().BeNull();
    }

    // ------------------------------------------------------------
    // CREATE INVARIANTS
    // ------------------------------------------------------------

    [Fact]
    public async Task CreateRootAsync_WhenVersionIsNotZero_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var root = UAuthSessionRoot.Create(
            Tenant,
            UserKey.New(),
            Now);

        root.Version = 1;

        var act = () => store.ExecuteAsync(
            ct => store.CreateRootAsync(
                root,
                ct));

        await act.Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateRootAsync_WhenUserAlreadyHasRoot_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        await CreateRootAsync(
            store,
            Tenant,
            user);

        var secondRoot =
            UAuthSessionRoot.Create(
                Tenant,
                user,
                Now.AddMinutes(1));

        var act = () => store.ExecuteAsync(
            ct => store.CreateRootAsync(
                secondRoot,
                ct));

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    // ------------------------------------------------------------
    // SAVE
    // ------------------------------------------------------------

    [Fact]
    public async Task SaveRootAsync_WhenExpectedVersionMatches_PersistsSecurityVersionIncrease()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var root =
            await CreateRootAsync(
                store,
                Tenant);

        var changedAt =
            Now.AddMinutes(10);

        var updated =
            root.IncreaseSecurityVersion(
                changedAt);

        await store.ExecuteAsync(
            ct => store.SaveRootAsync(
                updated,
                expectedVersion: root.Version,
                ct));

        var result =
            await store.GetRootByUserAsync(
                root.UserKey);

        result.Should().NotBeNull();

        result!.RootId.Should().Be(root.RootId);

        result.SecurityVersion
            .Should().Be(
                root.SecurityVersion + 1);

        result.UpdatedAt
            .Should().Be(changedAt);

        result.Version
            .Should().Be(
                root.Version + 1);

        result.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task SaveRootAsync_WhenRootDoesNotExist_ThrowsNotFound()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var root =
            UAuthSessionRoot.Create(
                Tenant,
                UserKey.New(),
                Now);

        var updated =
            root.IncreaseSecurityVersion(
                Now.AddMinutes(10));

        var act = () => store.ExecuteAsync(
            ct => store.SaveRootAsync(
                updated,
                expectedVersion: root.Version,
                ct));

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();
    }

    [Fact]
    public async Task SaveRootAsync_WhenExpectedVersionIsStale_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var root =
            await CreateRootAsync(
                store,
                Tenant);

        var updated =
            root.IncreaseSecurityVersion(
                Now.AddMinutes(10));

        var act = () => store.ExecuteAsync(
            ct => store.SaveRootAsync(
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
    public async Task RevokeRootAsync_WhenRootExists_RevokesRoot()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var root =
            await CreateRootAsync(
                store,
                Tenant);

        var revokedAt =
            Now.AddMinutes(10);

        await store.ExecuteAsync(
            ct => store.RevokeRootAsync(
                root.UserKey,
                revokedAt,
                ct));

        var result =
            await store.GetRootByUserAsync(
                root.UserKey);

        result.Should().NotBeNull();

        result!.IsRevoked.Should().BeTrue();
        result.RevokedAt.Should().Be(revokedAt);
        result.UpdatedAt.Should().Be(revokedAt);

        result.SecurityVersion
            .Should().Be(
                root.SecurityVersion + 1);

        result.Version
            .Should().Be(
                root.Version + 1);
    }

    [Fact]
    public async Task RevokeRootAsync_WhenRootDoesNotExist_IsIdempotent()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var act = () => store.ExecuteAsync(
            ct => store.RevokeRootAsync(
                UserKey.New(),
                Now,
                ct));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeRootAsync_WhenAlreadyRevoked_DoesNotMutateAgain()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var root =
            await CreateRootAsync(
                store,
                Tenant);

        var firstRevokedAt =
            Now.AddMinutes(5);

        await store.ExecuteAsync(
            ct => store.RevokeRootAsync(
                root.UserKey,
                firstRevokedAt,
                ct));

        var afterFirst =
            await store.GetRootByUserAsync(
                root.UserKey);

        afterFirst.Should().NotBeNull();

        await store.ExecuteAsync(
            ct => store.RevokeRootAsync(
                root.UserKey,
                Now.AddMinutes(10),
                ct));

        var afterSecond =
            await store.GetRootByUserAsync(
                root.UserKey);

        afterSecond.Should().NotBeNull();

        afterSecond!.RevokedAt
            .Should().Be(firstRevokedAt);

        afterSecond.UpdatedAt
            .Should().Be(firstRevokedAt);

        afterSecond.SecurityVersion
            .Should().Be(
                afterFirst!.SecurityVersion);

        afterSecond.Version
            .Should().Be(
                afterFirst.Version);
    }

    // ------------------------------------------------------------
    // TENANT ISOLATION - READ
    // ------------------------------------------------------------

    [Fact]
    public async Task GetRootByUserAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA =
            TenantKey.FromExternal("tenant-a");

        var tenantB =
            TenantKey.FromExternal("tenant-b");

        var storeA = db.CreateStore(tenantA);
        var storeB = db.CreateStore(tenantB);

        var user = UserKey.New();

        var root =
            await CreateRootAsync(
                storeA,
                tenantA,
                user);

        var fromA =
            await storeA.GetRootByUserAsync(user);

        var fromB =
            await storeB.GetRootByUserAsync(user);

        fromA.Should().NotBeNull();
        fromA!.RootId.Should().Be(root.RootId);

        fromB.Should().BeNull();
    }

    [Fact]
    public async Task GetRootByIdAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA =
            TenantKey.FromExternal("tenant-a");

        var tenantB =
            TenantKey.FromExternal("tenant-b");

        var storeA = db.CreateStore(tenantA);
        var storeB = db.CreateStore(tenantB);

        var root =
            await CreateRootAsync(
                storeA,
                tenantA);

        var fromA =
            await storeA.GetRootByIdAsync(
                root.RootId);

        var fromB =
            await storeB.GetRootByIdAsync(
                root.RootId);

        fromA.Should().NotBeNull();
        fromB.Should().BeNull();
    }

    // ------------------------------------------------------------
    // TENANT ISOLATION - WRITE
    // ------------------------------------------------------------

    [Fact]
    public async Task CreateRootAsync_WhenRootBelongsToDifferentTenant_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA =
            TenantKey.FromExternal("tenant-a");

        var tenantB =
            TenantKey.FromExternal("tenant-b");

        var storeA = db.CreateStore(tenantA);

        var rootB =
            UAuthSessionRoot.Create(
                tenantB,
                UserKey.New(),
                Now);

        var act = () => storeA.ExecuteAsync(
            ct => storeA.CreateRootAsync(
                rootB,
                ct));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Tenant mismatch.");
    }

    [Fact]
    public async Task SaveRootAsync_WhenRootBelongsToDifferentTenant_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA =
            TenantKey.FromExternal("tenant-a");

        var tenantB =
            TenantKey.FromExternal("tenant-b");

        var storeA = db.CreateStore(tenantA);

        var rootB =
            UAuthSessionRoot.Create(
                tenantB,
                UserKey.New(),
                Now);

        var updatedRootB =
            rootB.IncreaseSecurityVersion(
                Now.AddMinutes(10));

        var act = () => storeA.ExecuteAsync(
            ct => storeA.SaveRootAsync(
                updatedRootB,
                expectedVersion: rootB.Version,
                ct));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Tenant mismatch.");
    }

    // ------------------------------------------------------------
    // CANCELLATION
    // ------------------------------------------------------------

    [Fact]
    public async Task GetRootByUserAsync_WhenCancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        using var cts =
            new CancellationTokenSource();

        cts.Cancel();

        var act = () =>
            store.GetRootByUserAsync(
                UserKey.New(),
                cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }
}
