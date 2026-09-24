using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Reference;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;

public abstract class UserLifecycleStoreContractTests
{
    protected abstract Task<IUserLifecycleStoreTestDatabase> CreateDatabaseAsync();

    protected static readonly TenantKey TenantA =
        TenantKey.FromExternal("tenant-a");

    protected static readonly TenantKey TenantB =
        TenantKey.FromExternal("tenant-b");

    protected static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    protected static UserLifecycle CreateLifecycle(
        TenantKey tenant,
        UserKey? userKey = null,
        DateTimeOffset? createdAt = null)
    {
        return UserLifecycle.Create(
            tenant,
            userKey ?? UserKey.New(),
            createdAt ?? Now);
    }

    // ============================================================
    // ADD / GET / EXISTS
    // ============================================================

    [Fact]
    public async Task AddAsync_PersistsLifecycle()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var lifecycle = CreateLifecycle(TenantA);

        await store.AddAsync(lifecycle);

        var persisted = await store.GetAsync(
            new UserLifecycleKey(TenantA, lifecycle.UserKey));

        persisted.Should().NotBeNull();
        persisted!.Id.Should().Be(lifecycle.Id);
        persisted.Tenant.Should().Be(TenantA);
        persisted.UserKey.Should().Be(lifecycle.UserKey);
        persisted.Status.Should().Be(UserStatus.Active);
        persisted.SecurityVersion.Should().Be(0);
        persisted.CreatedAt.Should().Be(Now);
        persisted.Version.Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_WhenMissing_ReturnsNull()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var result = await store.GetAsync(
            new UserLifecycleKey(TenantA, UserKey.New()));

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenPresent_ReturnsTrue()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var lifecycle = CreateLifecycle(TenantA);
        await store.AddAsync(lifecycle);

        var result = await store.ExistsAsync(
            new UserLifecycleKey(TenantA, lifecycle.UserKey));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenMissing_ReturnsFalse()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var result = await store.ExistsAsync(
            new UserLifecycleKey(TenantA, UserKey.New()));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_WhenSameUserAlreadyExists_ThrowsConflict()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        await store.AddAsync(CreateLifecycle(TenantA, user));

        var act = () => store.AddAsync(
            CreateLifecycle(TenantA, user));

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task AddAsync_WhenVersionIsNotZero_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var lifecycle = CreateLifecycle(TenantA);
        lifecycle.Version = 42;

        var act = () => store.AddAsync(lifecycle);

        await act.Should()
            .ThrowAsync<InvalidOperationException>();
    }

    // ============================================================
    // SAVE
    // ============================================================

    [Fact]
    public async Task SaveAsync_PersistsDomainChanges_AndIncrementsVersion()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var lifecycle = CreateLifecycle(TenantA);
        await store.AddAsync(lifecycle);

        var changedAt = Now.AddMinutes(10);

        lifecycle.ChangeStatus(
            changedAt,
            UserStatus.Suspended);

        lifecycle.IncrementSecurityVersion();

        await store.SaveAsync(
            lifecycle,
            expectedVersion: 0);

        var persisted = await store.GetAsync(
            new UserLifecycleKey(TenantA, lifecycle.UserKey));

        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(UserStatus.Suspended);
        persisted.SecurityVersion.Should().Be(1);
        persisted.UpdatedAt.Should().Be(changedAt);
        persisted.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_WhenMissing_ThrowsNotFound()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var lifecycle = CreateLifecycle(TenantA);

        var act = () => store.SaveAsync(
            lifecycle,
            expectedVersion: 0);

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();
    }

    [Fact]
    public async Task SaveAsync_WhenVersionIsStale_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var lifecycle = CreateLifecycle(TenantA);
        await store.AddAsync(lifecycle);

        lifecycle.IncrementSecurityVersion();

        await store.SaveAsync(lifecycle, 0);

        lifecycle.IncrementSecurityVersion();

        var act = () => store.SaveAsync(
            lifecycle,
            expectedVersion: 0);

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    // ============================================================
    // DELETE
    // ============================================================

    [Fact]
    public async Task DeleteAsync_Soft_PreservesRecordAndDeletionState()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var lifecycle = CreateLifecycle(TenantA);
        var key = new UserLifecycleKey(TenantA, lifecycle.UserKey);

        await store.AddAsync(lifecycle);

        await store.DeleteAsync(
            key,
            expectedVersion: 0,
            DeleteMode.Soft,
            Now.AddHours(1));

        var persisted = await store.GetAsync(key);

        persisted.Should().NotBeNull();
        persisted!.IsDeleted.Should().BeTrue();
        persisted.DeletedAt.Should().Be(Now.AddHours(1));
        persisted.Version.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_Hard_RemovesRecord()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var lifecycle = CreateLifecycle(TenantA);
        var key = new UserLifecycleKey(TenantA, lifecycle.UserKey);

        await store.AddAsync(lifecycle);

        await store.DeleteAsync(
            key,
            expectedVersion: 0,
            DeleteMode.Hard,
            Now);

        (await store.GetAsync(key)).Should().BeNull();
        (await store.ExistsAsync(key)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ThrowsNotFound()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var act = () => store.DeleteAsync(
            new UserLifecycleKey(TenantA, UserKey.New()),
            0,
            DeleteMode.Soft,
            Now);

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenVersionIsStale_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var lifecycle = CreateLifecycle(TenantA);
        var key = new UserLifecycleKey(TenantA, lifecycle.UserKey);

        await store.AddAsync(lifecycle);

        lifecycle.IncrementSecurityVersion();
        await store.SaveAsync(lifecycle, 0);

        var act = () => store.DeleteAsync(
            key,
            expectedVersion: 0,
            DeleteMode.Soft,
            Now);

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    // ============================================================
    // TENANT
    // ============================================================

    [Fact]
    public async Task AddAsync_WhenEntityBelongsToDifferentTenant_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();

        var store = db.CreateStore(TenantA);
        var lifecycle = CreateLifecycle(TenantB);

        var act = () => store.AddAsync(lifecycle);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task SaveAsync_WhenEntityBelongsToDifferentTenant_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();

        var storeA = db.CreateStore(TenantA);
        var storeB = db.CreateStore(TenantB);

        var lifecycle = CreateLifecycle(TenantB);
        await storeB.AddAsync(lifecycle);

        var act = () => storeA.SaveAsync(
            lifecycle,
            expectedVersion: 0);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task QueryAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var storeA = db.CreateStore(TenantA);
        var storeB = db.CreateStore(TenantB);

        await storeA.AddAsync(CreateLifecycle(TenantA));
        await storeB.AddAsync(CreateLifecycle(TenantB));

        var resultA = await storeA.QueryAsync(
            new UserLifecycleQuery());

        var resultB = await storeB.QueryAsync(
            new UserLifecycleQuery());

        resultA.Items.Should().ContainSingle();
        resultA.Items.Single().Tenant.Should().Be(TenantA);

        resultB.Items.Should().ContainSingle();
        resultB.Items.Single().Tenant.Should().Be(TenantB);
    }

    // ============================================================
    // QUERY - DELETION
    // ============================================================

    [Fact]
    public async Task QueryAsync_ByDefault_ExcludesDeleted()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var active = CreateLifecycle(TenantA);
        var deleted = CreateLifecycle(TenantA);

        await store.AddAsync(active);
        await store.AddAsync(deleted);

        await store.DeleteAsync(
            new UserLifecycleKey(TenantA, deleted.UserKey),
            0,
            DeleteMode.Soft,
            Now.AddHours(1));

        var result = await store.QueryAsync(
            new UserLifecycleQuery());

        result.Items.Should().ContainSingle();
        result.Items.Single().UserKey.Should().Be(active.UserKey);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_IncludeDeleted_ReturnsDeletedRecords()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var active = CreateLifecycle(TenantA);
        var deleted = CreateLifecycle(TenantA);

        await store.AddAsync(active);
        await store.AddAsync(deleted);

        await store.DeleteAsync(
            new UserLifecycleKey(TenantA, deleted.UserKey),
            0,
            DeleteMode.Soft,
            Now.AddHours(1));

        var result = await store.QueryAsync(
            new UserLifecycleQuery
            {
                IncludeDeleted = true
            });

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);

        result.Items.Should()
            .Contain(x => x.UserKey == deleted.UserKey && x.IsDeleted);
    }

    // ============================================================
    // QUERY - STATUS
    // ============================================================

    [Fact]
    public async Task QueryAsync_Status_FiltersResults()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var active = CreateLifecycle(TenantA);

        var suspended = CreateLifecycle(TenantA);
        suspended.ChangeStatus(
            Now.AddMinutes(1),
            UserStatus.Suspended);

        await store.AddAsync(active);
        await store.AddAsync(suspended);

        var result = await store.QueryAsync(
            new UserLifecycleQuery
            {
                Status = UserStatus.Suspended
            });

        result.Items.Should().ContainSingle();
        result.Items.Single().UserKey.Should().Be(suspended.UserKey);
        result.TotalCount.Should().Be(1);
    }

    // ============================================================
    // QUERY - PAGINATION
    // ============================================================

    [Fact]
    public async Task QueryAsync_AppliesPagination()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        for (var i = 0; i < 5; i++)
        {
            await store.AddAsync(
                CreateLifecycle(
                    TenantA,
                    createdAt: Now.AddMinutes(i)));
        }

        var result = await store.QueryAsync(
            new UserLifecycleQuery
            {
                PageNumber = 2,
                PageSize = 2,
                SortBy = nameof(UserLifecycle.CreatedAt)
            });

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);

        result.Items[0].CreatedAt.Should().Be(Now.AddMinutes(2));
        result.Items[1].CreatedAt.Should().Be(Now.AddMinutes(3));
    }

    // ============================================================
    // QUERY - SORT
    // ============================================================

    [Fact]
    public async Task QueryAsync_SortsCreatedAtAscending()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var third = CreateLifecycle(
            TenantA,
            createdAt: Now.AddMinutes(3));

        var first = CreateLifecycle(
            TenantA,
            createdAt: Now.AddMinutes(1));

        var second = CreateLifecycle(
            TenantA,
            createdAt: Now.AddMinutes(2));

        await store.AddAsync(third);
        await store.AddAsync(first);
        await store.AddAsync(second);

        var result = await store.QueryAsync(
            new UserLifecycleQuery
            {
                SortBy = nameof(UserLifecycle.CreatedAt),
                Descending = false
            });

        result.Items.Select(x => x.CreatedAt)
            .Should()
            .ContainInOrder(
                Now.AddMinutes(1),
                Now.AddMinutes(2),
                Now.AddMinutes(3));
    }

    [Fact]
    public async Task QueryAsync_SortsCreatedAtDescending()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        await store.AddAsync(
            CreateLifecycle(TenantA, createdAt: Now.AddMinutes(1)));

        await store.AddAsync(
            CreateLifecycle(TenantA, createdAt: Now.AddMinutes(3)));

        await store.AddAsync(
            CreateLifecycle(TenantA, createdAt: Now.AddMinutes(2)));

        var result = await store.QueryAsync(
            new UserLifecycleQuery
            {
                SortBy = nameof(UserLifecycle.CreatedAt),
                Descending = true
            });

        result.Items.Select(x => x.CreatedAt)
            .Should()
            .ContainInOrder(
                Now.AddMinutes(3),
                Now.AddMinutes(2),
                Now.AddMinutes(1));
    }

    [Fact]
    public async Task QueryAsync_SortByUserKey_IsSupported()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var firstUser = UserKey.FromString("user-a");
        var secondUser = UserKey.FromString("user-b");
        var thirdUser = UserKey.FromString("user-c");

        await store.AddAsync(CreateLifecycle(TenantA, thirdUser));
        await store.AddAsync(CreateLifecycle(TenantA, firstUser));
        await store.AddAsync(CreateLifecycle(TenantA, secondUser));

        var result = await store.QueryAsync(
            new UserLifecycleQuery
            {
                SortBy = nameof(UserLifecycle.UserKey),
                Descending = false
            });

        result.Items.Select(x => x.UserKey.Value)
            .Should()
            .ContainInOrder(
                firstUser.Value,
                secondUser.Value,
                thirdUser.Value);
    }

    [Fact]
    public async Task QueryAsync_SortByDeletedAt_IsSupported()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var first = CreateLifecycle(TenantA);
        var second = CreateLifecycle(TenantA);

        await store.AddAsync(first);
        await store.AddAsync(second);

        await store.DeleteAsync(
            new UserLifecycleKey(TenantA, first.UserKey),
            0,
            DeleteMode.Soft,
            Now.AddHours(1));

        await store.DeleteAsync(
            new UserLifecycleKey(TenantA, second.UserKey),
            0,
            DeleteMode.Soft,
            Now.AddHours(2));

        var result = await store.QueryAsync(
            new UserLifecycleQuery
            {
                IncludeDeleted = true,
                SortBy = nameof(UserLifecycle.DeletedAt),
                Descending = false
            });

        result.Items.Select(x => x.DeletedAt)
            .Should()
            .ContainInOrder(
                Now.AddHours(1),
                Now.AddHours(2));
    }

    // ============================================================
    // CANCELLATION
    // ============================================================

    [Fact]
    public async Task GetAsync_WhenCancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.GetAsync(
            new UserLifecycleKey(TenantA, UserKey.New()),
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AddAsync_WhenCancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.AddAsync(
            CreateLifecycle(TenantA),
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryAsync_WhenCancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.QueryAsync(
            new UserLifecycleQuery(),
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }
}