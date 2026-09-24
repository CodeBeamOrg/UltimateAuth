using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Contracts.Authorization;

public abstract class RoleStoreContractTests
{
    protected abstract Task<IRoleStoreTestDatabase> CreateDatabaseAsync();

    protected virtual TenantKey Tenant =>
        TenantKey.Single;

    // ---------------------------------------------------------
    // Add / Get
    // ---------------------------------------------------------

    [Fact]
    public async Task AddAsync_WhenValid_PersistsRole()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var role = CreateRole("Administrators");

        await store.AddAsync(role);

        var result = await store.GetAsync(
            new RoleKey(Tenant, role.Id));

        result.Should().NotBeNull();
        result!.Id.Should().Be(role.Id);
        result.Tenant.Should().Be(Tenant);
        result.Name.Should().Be(role.Name);
        result.NormalizedName.Should().Be(role.NormalizedName);
        result.Permissions.Should().BeEquivalentTo(role.Permissions);
    }

    [Fact]
    public async Task AddAsync_WhenActiveNormalizedNameAlreadyExists_ThrowsConflict()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var first = CreateRole("Administrators");
        var duplicate = CreateRole("  administrators  ");

        await store.AddAsync(first);

        var act = () => store.AddAsync(duplicate);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task AddAsync_WhenDeletedRoleHasSameNormalizedName_ThrowsConflict()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var first = CreateRole("Administrators");

        await store.AddAsync(first);

        await store.DeleteAsync(
            new RoleKey(Tenant, first.Id),
            first.Version,
            DeleteMode.Soft,
            Now);

        var replacement = CreateRole("Administrators");

        var act = () => store.AddAsync(replacement);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task AddAsync_WhenPreviousRoleWasHardDeleted_AllowsNameReuse()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var first = CreateRole("Administrators");

        await store.AddAsync(first);

        await store.DeleteAsync(
            new RoleKey(Tenant, first.Id),
            first.Version,
            DeleteMode.Hard,
            Now);

        var replacement = CreateRole("Administrators");

        var act = () => store.AddAsync(replacement);

        await act.Should().NotThrowAsync();

        var result = await store.GetByNameAsync(
            replacement.NormalizedName);

        result.Should().NotBeNull();
        result!.Id.Should().Be(replacement.Id);
    }

    // ---------------------------------------------------------
    // Name lookup
    // ---------------------------------------------------------

    [Fact]
    public async Task GetByNameAsync_WhenRoleExists_ReturnsRole()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var role = CreateRole("Administrators");

        await store.AddAsync(role);

        var result = await store.GetByNameAsync(
            role.NormalizedName);

        result.Should().NotBeNull();
        result!.Id.Should().Be(role.Id);
        result.Name.Should().Be(role.Name);
    }

    [Fact]
    public async Task GetByNameAsync_WhenRoleIsDeleted_ReturnsNull()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var role = CreateRole("Administrators");

        await store.AddAsync(role);

        await store.DeleteAsync(
            new RoleKey(Tenant, role.Id),
            role.Version,
            DeleteMode.Soft,
            Now);

        var result = await store.GetByNameAsync(
            role.NormalizedName);

        result.Should().BeNull();
    }

    // ---------------------------------------------------------
    // GetByIds
    // ---------------------------------------------------------

    [Fact]
    public async Task GetByIdsAsync_ReturnsOnlyRequestedRoles()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var admin = CreateRole("Administrators");
        var operatorRole = CreateRole("Operators");
        var unrelated = CreateRole("Auditors");

        await store.AddAsync(admin);
        await store.AddAsync(operatorRole);
        await store.AddAsync(unrelated);

        var result = await store.GetByIdsAsync(
            [admin.Id, operatorRole.Id]);

        result.Select(x => x.Id)
            .Should()
            .BeEquivalentTo([admin.Id, operatorRole.Id]);

        result.Should()
            .NotContain(x => x.Id == unrelated.Id);
    }

    [Fact]
    public async Task GetByIdsAsync_DoesNotReturnDeletedRoles()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var active = CreateRole("Active");
        var deleted = CreateRole("Deleted");

        await store.AddAsync(active);
        await store.AddAsync(deleted);

        await store.DeleteAsync(
            new RoleKey(Tenant, deleted.Id),
            deleted.Version,
            DeleteMode.Soft,
            Now);

        var result = await store.GetByIdsAsync(
            [active.Id, deleted.Id]);

        result.Should().ContainSingle();
        result.Single().Id.Should().Be(active.Id);
    }

    // ---------------------------------------------------------
    // Save / concurrency
    // ---------------------------------------------------------

    [Fact]
    public async Task SaveAsync_WhenRoleIsChanged_PersistsChanges()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var role = CreateRole("Administrators");

        await store.AddAsync(role);

        var persisted = await store.GetAsync(
            new RoleKey(Tenant, role.Id));

        persisted.Should().NotBeNull();

        var expectedVersion = persisted!.Version;

        persisted.Rename("Operators", Now.AddMinutes(1));

        await store.SaveAsync(
            persisted,
            expectedVersion);

        var result = await store.GetAsync(
            new RoleKey(Tenant, role.Id));

        result.Should().NotBeNull();
        result!.Name.Should().Be("Operators");
        result.Version.Should().BeGreaterThan(expectedVersion);
    }

    [Fact]
    public async Task SaveAsync_WhenExpectedVersionIsStale_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var role = CreateRole("Administrators");

        await store.AddAsync(role);

        var persisted = await store.GetAsync(
            new RoleKey(Tenant, role.Id));

        persisted.Should().NotBeNull();

        persisted!.Rename(
            "Operators",
            Now.AddMinutes(1));

        var staleVersion = persisted.Version + 100;

        var act = () => store.SaveAsync(
            persisted,
            staleVersion);

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    [Fact]
    public async Task SaveAsync_WhenRenamedToExistingActiveRole_ThrowsConflict()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var admin = CreateRole("Administrators");
        var operators = CreateRole("Operators");

        await store.AddAsync(admin);
        await store.AddAsync(operators);

        var persisted = await store.GetAsync(
            new RoleKey(Tenant, operators.Id));

        persisted.Should().NotBeNull();

        var expectedVersion = persisted!.Version;

        persisted.Rename(
            "Administrators",
            Now.AddMinutes(1));

        var act = () => store.SaveAsync(
            persisted,
            expectedVersion);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    // ---------------------------------------------------------
    // Delete
    // ---------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_WhenSoftDeleted_KeepsRoleButMarksDeleted()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var role = CreateRole("Administrators");

        await store.AddAsync(role);

        var deletedAt = Now.AddMinutes(1);

        await store.DeleteAsync(
            new RoleKey(Tenant, role.Id),
            role.Version,
            DeleteMode.Soft,
            deletedAt);

        var result = await store.GetAsync(
            new RoleKey(Tenant, role.Id));

        result.Should().NotBeNull();
        result!.IsDeleted.Should().BeTrue();
        result.DeletedAt.Should().Be(deletedAt);
    }

    [Fact]
    public async Task DeleteAsync_WhenHardDeleted_RemovesRole()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var role = CreateRole("Administrators");

        await store.AddAsync(role);

        await store.DeleteAsync(
            new RoleKey(Tenant, role.Id),
            role.Version,
            DeleteMode.Hard,
            Now);

        var result = await store.GetAsync(
            new RoleKey(Tenant, role.Id));

        result.Should().BeNull();

        var exists = await store.ExistsAsync(
            new RoleKey(Tenant, role.Id));

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenExpectedVersionIsStale_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var role = CreateRole("Administrators");

        await store.AddAsync(role);

        var act = () => store.DeleteAsync(
            new RoleKey(Tenant, role.Id),
            role.Version + 100,
            DeleteMode.Soft,
            Now);

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    // ---------------------------------------------------------
    // Query
    // ---------------------------------------------------------

    [Fact]
    public async Task QueryAsync_WhenIncludeDeletedIsFalse_ExcludesDeletedRoles()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var active = CreateRole("Active");
        var deleted = CreateRole("Deleted");

        await store.AddAsync(active);
        await store.AddAsync(deleted);

        await store.DeleteAsync(
            new RoleKey(Tenant, deleted.Id),
            deleted.Version,
            DeleteMode.Soft,
            Now);

        var result = await store.QueryAsync(
            new RoleQuery
            {
                IncludeDeleted = false
            });

        result.Items.Should()
            .Contain(x => x.Id == active.Id);

        result.Items.Should()
            .NotContain(x => x.Id == deleted.Id);
    }

    [Fact]
    public async Task QueryAsync_WhenIncludeDeletedIsTrue_IncludesDeletedRoles()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var active = CreateRole("Active");
        var deleted = CreateRole("Deleted");

        await store.AddAsync(active);
        await store.AddAsync(deleted);

        await store.DeleteAsync(
            new RoleKey(Tenant, deleted.Id),
            deleted.Version,
            DeleteMode.Soft,
            Now);

        var result = await store.QueryAsync(
            new RoleQuery
            {
                IncludeDeleted = true
            });

        result.Items.Select(x => x.Id)
            .Should()
            .Contain([active.Id, deleted.Id]);
    }

    [Fact]
    public async Task QueryAsync_WhenSearchSpecified_SearchesNormalizedName()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var admin = CreateRole("Global Administrators");
        var operators = CreateRole("Operators");

        await store.AddAsync(admin);
        await store.AddAsync(operators);

        var result = await store.QueryAsync(
            new RoleQuery
            {
                Search = " admin "
            });

        result.Items.Should().ContainSingle();
        result.Items.Single().Id.Should().Be(admin.Id);
    }

    [Fact]
    public async Task QueryAsync_SortsBeforeApplyingPagination()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        foreach (var name in new[]
        {
            "Charlie",
            "Alpha",
            "Echo",
            "Bravo",
            "Delta"
        })
        {
            await store.AddAsync(CreateRole(name));
        }

        var result = await store.QueryAsync(
            new RoleQuery
            {
                PageNumber = 2,
                PageSize = 2,
                SortBy = nameof(Role.Name),
                Descending = false
            });

        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);

        result.Items
            .Select(x => x.Name)
            .Should()
            .ContainInOrder("Charlie", "Delta");
    }

    // ---------------------------------------------------------
    // Tenant isolation
    // ---------------------------------------------------------

    [Fact]
    public async Task Store_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA = TestIds.Tenant("tenant-a");
        var tenantB = TestIds.Tenant("tenant-b");

        var storeA = db.CreateStore(tenantA);
        var storeB = db.CreateStore(tenantB);

        var roleA = CreateRole(
            "Administrators",
            tenantA);

        await storeA.AddAsync(roleA);

        var fromA = await storeA.GetAsync(
            new RoleKey(tenantA, roleA.Id));

        var fromB = await storeB.GetAsync(
            new RoleKey(tenantB, roleA.Id));

        fromA.Should().NotBeNull();
        fromB.Should().BeNull();

        var queryB = await storeB.QueryAsync(
            new RoleQuery
            {
                IncludeDeleted = true
            });

        queryB.Items.Should()
            .NotContain(x => x.Id == roleA.Id);
    }

    // ---------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------

    protected static readonly DateTimeOffset Now =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    protected Role CreateRole(
        string name,
        TenantKey? tenant = null)
    {
        return Role.Create(
            RoleId.New(),
            tenant ?? Tenant,
            name,
            [],
            Now);
    }
}
