using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;

public abstract class UserIdentifierStoreContractTests
{
    protected abstract Task<IUserIdentifierStoreTestDatabase> CreateDatabaseAsync();

    protected static readonly TenantKey TenantA =
        TenantKey.FromExternal("tenant-a");

    protected static readonly TenantKey TenantB =
        TenantKey.FromExternal("tenant-b");

    protected static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    protected static UserIdentifier CreateIdentifier(
        TenantKey tenant,
        UserKey? userKey = null,
        UserIdentifierType type = UserIdentifierType.Email,
        string value = "user@example.com",
        string normalizedValue = "USER@EXAMPLE.COM",
        bool isPrimary = false,
        DateTimeOffset? createdAt = null)
    {
        return UserIdentifier.Create(
            id: null,
            tenant: tenant,
            userKey: userKey ?? UserKey.New(),
            type: type,
            value: value,
            normalizedValue: normalizedValue,
            now: createdAt ?? Now,
            isPrimary: isPrimary);
    }

    // ============================================================
    // ADD / GET / EXISTS
    // ============================================================

    [Fact]
    public async Task AddAsync_PersistsIdentifier()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(
            TenantA,
            isPrimary: true);

        await store.AddAsync(identifier);

        var persisted = await store.GetByIdAsync(identifier.Id);

        persisted.Should().NotBeNull();
        persisted!.Id.Should().Be(identifier.Id);
        persisted.Tenant.Should().Be(TenantA);
        persisted.UserKey.Should().Be(identifier.UserKey);
        persisted.Type.Should().Be(identifier.Type);
        persisted.Value.Should().Be(identifier.Value);
        persisted.NormalizedValue.Should().Be(identifier.NormalizedValue);
        persisted.IsPrimary.Should().BeTrue();
        persisted.CreatedAt.Should().Be(Now);
        persisted.Version.Should().Be(0);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var result = await store.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ByTypeAndNormalizedValue_ReturnsIdentifier()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(TenantA);

        await store.AddAsync(identifier);

        var result = await store.GetAsync(
            UserIdentifierType.Email,
            "USER@EXAMPLE.COM");

        result.Should().NotBeNull();
        result!.Id.Should().Be(identifier.Id);
    }

    [Fact]
    public async Task GetAsync_ByTypeAndNormalizedValue_ExcludesDeleted()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(TenantA);

        await store.AddAsync(identifier);

        await store.DeleteAsync(
            identifier.Id,
            expectedVersion: 0,
            DeleteMode.Soft,
            Now.AddHours(1));

        var result = await store.GetAsync(
            identifier.Type,
            identifier.NormalizedValue);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyActiveIdentifiersForUser()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var userA = UserKey.New();
        var userB = UserKey.New();

        var first = CreateIdentifier(
            TenantA,
            userA,
            value: "a@example.com",
            normalizedValue: "A@EXAMPLE.COM");

        var second = CreateIdentifier(
            TenantA,
            userA,
            type: UserIdentifierType.Username,
            value: "alice",
            normalizedValue: "ALICE");

        var otherUser = CreateIdentifier(
            TenantA,
            userB,
            value: "b@example.com",
            normalizedValue: "B@EXAMPLE.COM");

        await store.AddAsync(first);
        await store.AddAsync(second);
        await store.AddAsync(otherUser);

        await store.DeleteAsync(
            second.Id,
            0,
            DeleteMode.Soft,
            Now.AddHours(1));

        var result = await store.GetByUserAsync(userA);

        result.Should().ContainSingle();
        result.Single().Id.Should().Be(first.Id);
    }

    // ============================================================
    // SAVE
    // ============================================================

    [Fact]
    public async Task SaveAsync_PersistsChangesAndIncrementsVersion()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(TenantA);

        await store.AddAsync(identifier);

        var changedAt = Now.AddMinutes(10);

        identifier.ChangeValue(
            "changed@example.com",
            "CHANGED@EXAMPLE.COM",
            changedAt);

        await store.SaveAsync(
            identifier,
            expectedVersion: 0);

        var persisted = await store.GetByIdAsync(identifier.Id);

        persisted.Should().NotBeNull();
        persisted!.Value.Should().Be("changed@example.com");
        persisted.NormalizedValue.Should().Be("CHANGED@EXAMPLE.COM");
        persisted.UpdatedAt.Should().Be(changedAt);
        persisted.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_WhenMissing_ThrowsNotFound()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(TenantA);

        var act = () => store.SaveAsync(identifier, 0);

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();
    }

    [Fact]
    public async Task SaveAsync_WhenVersionIsStale_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(TenantA);

        await store.AddAsync(identifier);

        identifier.MarkVerified(Now.AddMinutes(1));
        await store.SaveAsync(identifier, 0);

        var act = () => store.SaveAsync(
            identifier,
            expectedVersion: 0);

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    // ============================================================
    // PRIMARY
    // ============================================================

    [Fact]
    public async Task AddAsync_PrimaryIdentifier_UnsetsExistingPrimaryOfSameType()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var first = CreateIdentifier(
            TenantA,
            user,
            value: "first@example.com",
            normalizedValue: "FIRST@EXAMPLE.COM",
            isPrimary: true);

        var second = CreateIdentifier(
            TenantA,
            user,
            value: "second@example.com",
            normalizedValue: "SECOND@EXAMPLE.COM",
            isPrimary: true);

        await store.AddAsync(first);
        await store.AddAsync(second);

        var persistedFirst = await store.GetByIdAsync(first.Id);
        var persistedSecond = await store.GetByIdAsync(second.Id);

        persistedFirst!.IsPrimary.Should().BeFalse();
        persistedSecond!.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_PrimaryIdentifier_DoesNotUnsetPrimaryOfDifferentType()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var email = CreateIdentifier(
            TenantA,
            user,
            UserIdentifierType.Email,
            "a@example.com",
            "A@EXAMPLE.COM",
            isPrimary: true);

        var username = CreateIdentifier(
            TenantA,
            user,
            UserIdentifierType.Username,
            "alice",
            "ALICE",
            isPrimary: true);

        await store.AddAsync(email);
        await store.AddAsync(username);

        (await store.GetByIdAsync(email.Id))!
            .IsPrimary.Should().BeTrue();

        (await store.GetByIdAsync(username.Id))!
            .IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_SetPrimary_UnsetsExistingPrimaryOfSameType()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var first = CreateIdentifier(
            TenantA,
            user,
            value: "first@example.com",
            normalizedValue: "FIRST@EXAMPLE.COM",
            isPrimary: true);

        var second = CreateIdentifier(
            TenantA,
            user,
            value: "second@example.com",
            normalizedValue: "SECOND@EXAMPLE.COM");

        await store.AddAsync(first);
        await store.AddAsync(second);

        second.SetPrimary(Now.AddMinutes(1));

        await store.SaveAsync(second, 0);

        (await store.GetByIdAsync(first.Id))!
            .IsPrimary.Should().BeFalse();

        (await store.GetByIdAsync(second.Id))!
            .IsPrimary.Should().BeTrue();
    }

    // ============================================================
    // DELETE
    // ============================================================

    [Fact]
    public async Task DeleteAsync_Soft_PreservesRecordAndClearsPrimary()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(
            TenantA,
            isPrimary: true);

        await store.AddAsync(identifier);

        var deletedAt = Now.AddHours(1);

        await store.DeleteAsync(
            identifier.Id,
            0,
            DeleteMode.Soft,
            deletedAt);

        var persisted = await store.GetByIdAsync(identifier.Id);

        persisted.Should().NotBeNull();
        persisted!.IsDeleted.Should().BeTrue();
        persisted.DeletedAt.Should().Be(deletedAt);
        persisted.IsPrimary.Should().BeFalse();
        persisted.Version.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_Hard_RemovesIdentifier()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(TenantA);

        await store.AddAsync(identifier);

        await store.DeleteAsync(
            identifier.Id,
            0,
            DeleteMode.Hard,
            Now);

        (await store.GetByIdAsync(identifier.Id))
            .Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenVersionIsStale_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(TenantA);

        await store.AddAsync(identifier);

        identifier.MarkVerified(Now.AddMinutes(1));
        await store.SaveAsync(identifier, 0);

        var act = () => store.DeleteAsync(
            identifier.Id,
            expectedVersion: 0,
            DeleteMode.Soft,
            Now.AddHours(1));

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
        var identifier = CreateIdentifier(TenantB);

        var act = () => store.AddAsync(identifier);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task SaveAsync_WhenEntityBelongsToDifferentTenant_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();

        var storeA = db.CreateStore(TenantA);
        var storeB = db.CreateStore(TenantB);

        var identifier = CreateIdentifier(TenantB);
        await storeB.AddAsync(identifier);

        var act = () => storeA.SaveAsync(
            identifier,
            expectedVersion: 0);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task GetByIdAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var storeA = db.CreateStore(TenantA);
        var storeB = db.CreateStore(TenantB);

        var identifier = CreateIdentifier(TenantB);
        await storeB.AddAsync(identifier);

        var result = await storeA.GetByIdAsync(identifier.Id);

        result.Should().BeNull();
    }

    // ============================================================
    // CANCELLATION
    // ============================================================

    [Fact]
    public async Task AddAsync_WhenCancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.AddAsync(
            CreateIdentifier(TenantA),
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.GetByIdAsync(
            Guid.NewGuid(),
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
            new UserIdentifierQuery
            {
                UserKey = UserKey.New()
            },
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExistsAsync_TenantAny_FindsMatchingIdentifier()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(TenantA);
        await store.AddAsync(identifier);

        var result = await store.ExistsAsync(
    new IdentifierExistenceQuery(
        identifier.Type,
        identifier.NormalizedValue,
        IdentifierExistenceScope.TenantAny));

        result.Exists.Should().BeTrue();
        result.OwnerUserKey.Should().Be(identifier.UserKey);
        result.OwnerIdentifierId.Should().Be(identifier.Id);
    }

    [Fact]
    public async Task ExistsAsync_WithinUser_DoesNotMatchOtherUser()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(TenantA);
        await store.AddAsync(identifier);

        var result = await store.ExistsAsync(
    new IdentifierExistenceQuery(
        identifier.Type,
        identifier.NormalizedValue,
        IdentifierExistenceScope.WithinUser,
        UserKey: UserKey.New()));

        result.Exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_TenantPrimaryOnly_IgnoresNonPrimary()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(
            TenantA,
            isPrimary: false);

        await store.AddAsync(identifier);

        var result = await store.ExistsAsync(
    new IdentifierExistenceQuery(
        identifier.Type,
        identifier.NormalizedValue,
        IdentifierExistenceScope.TenantPrimaryOnly));

        result.Exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ExcludeIdentifierId_ExcludesCurrentIdentifier()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(TenantA);
        await store.AddAsync(identifier);

        var result = await store.ExistsAsync(
    new IdentifierExistenceQuery(
        identifier.Type,
        identifier.NormalizedValue,
        IdentifierExistenceScope.TenantAny,
        ExcludeIdentifierId: identifier.Id));

        result.Exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_DoesNotMatchDeletedIdentifier()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var identifier = CreateIdentifier(TenantA);
        await store.AddAsync(identifier);

        await store.DeleteAsync(
            identifier.Id,
            0,
            DeleteMode.Soft,
            Now.AddHours(1));

        var result = await store.ExistsAsync(
        new IdentifierExistenceQuery(
            identifier.Type,
            identifier.NormalizedValue,
            IdentifierExistenceScope.TenantAny));

        result.Exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetByUsersAsync_ReturnsActiveIdentifiersForRequestedUsersOnly()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var userA = UserKey.New();
        var userB = UserKey.New();
        var userC = UserKey.New();

        var a = CreateIdentifier(
            TenantA, userA,
            value: "a@example.com",
            normalizedValue: "A@EXAMPLE.COM");

        var b = CreateIdentifier(
            TenantA, userB,
            value: "b@example.com",
            normalizedValue: "B@EXAMPLE.COM");

        var c = CreateIdentifier(
            TenantA, userC,
            value: "c@example.com",
            normalizedValue: "C@EXAMPLE.COM");

        await store.AddAsync(a);
        await store.AddAsync(b);
        await store.AddAsync(c);

        var result = await store.GetByUsersAsync(
            [userA, userB]);

        result.Should().HaveCount(2);
        result.Select(x => x.UserKey)
            .Should()
            .BeEquivalentTo([userA, userB]);
    }

    [Fact]
    public async Task DeleteByUserAsync_Soft_DeletesOnlySpecifiedUsersIdentifiers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var userA = UserKey.New();
        var userB = UserKey.New();

        var a1 = CreateIdentifier(
            TenantA, userA,
            value: "a1@example.com",
            normalizedValue: "A1@EXAMPLE.COM",
            isPrimary: true);

        var a2 = CreateIdentifier(
            TenantA, userA,
            UserIdentifierType.Username,
            "alice",
            "ALICE",
            isPrimary: true);

        var b = CreateIdentifier(
            TenantA, userB,
            value: "b@example.com",
            normalizedValue: "B@EXAMPLE.COM");

        await store.AddAsync(a1);
        await store.AddAsync(a2);
        await store.AddAsync(b);

        var deletedAt = Now.AddHours(1);

        await store.DeleteByUserAsync(
            userA,
            DeleteMode.Soft,
            deletedAt);

        var query = await store.QueryAsync(
            new UserIdentifierQuery
            {
                UserKey = userA,
                IncludeDeleted = true
            });

        query.Items.Should().HaveCount(2);
        query.Items.Should().OnlyContain(x =>
            x.IsDeleted &&
            x.DeletedAt == deletedAt &&
            !x.IsPrimary);

        (await store.GetByUserAsync(userB))
            .Should()
            .ContainSingle();
    }

    [Fact]
    public async Task DeleteByUserAsync_Hard_RemovesOnlySpecifiedUsersIdentifiers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var userA = UserKey.New();
        var userB = UserKey.New();

        var a = CreateIdentifier(
            TenantA, userA,
            value: "a@example.com",
            normalizedValue: "A@EXAMPLE.COM");

        var b = CreateIdentifier(
            TenantA, userB,
            value: "b@example.com",
            normalizedValue: "B@EXAMPLE.COM");

        await store.AddAsync(a);
        await store.AddAsync(b);

        await store.DeleteByUserAsync(
            userA,
            DeleteMode.Hard,
            Now);

        (await store.GetByIdAsync(a.Id)).Should().BeNull();
        (await store.GetByIdAsync(b.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task QueryAsync_WhenUserKeyMissing_ThrowsValidation()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var act = () => store.QueryAsync(
            new UserIdentifierQuery());

        await act.Should()
            .ThrowAsync<UAuthIdentifierValidationException>();
    }

    [Fact]
    public async Task QueryAsync_ByDefault_ExcludesDeleted()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var active = CreateIdentifier(
            TenantA, user,
            value: "active@example.com",
            normalizedValue: "ACTIVE@EXAMPLE.COM");

        var deleted = CreateIdentifier(
            TenantA, user,
            value: "deleted@example.com",
            normalizedValue: "DELETED@EXAMPLE.COM");

        await store.AddAsync(active);
        await store.AddAsync(deleted);

        await store.DeleteAsync(
            deleted.Id,
            0,
            DeleteMode.Soft,
            Now.AddHours(1));

        var result = await store.QueryAsync(
            new UserIdentifierQuery
            {
                UserKey = user
            });

        result.Items.Should().ContainSingle();
        result.Items.Single().Id.Should().Be(active.Id);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_IncludeDeleted_ReturnsDeletedIdentifiers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var active = CreateIdentifier(
            TenantA, user,
            value: "active@example.com",
            normalizedValue: "ACTIVE@EXAMPLE.COM");

        var deleted = CreateIdentifier(
            TenantA, user,
            value: "deleted@example.com",
            normalizedValue: "DELETED@EXAMPLE.COM");

        await store.AddAsync(active);
        await store.AddAsync(deleted);

        await store.DeleteAsync(
            deleted.Id,
            0,
            DeleteMode.Soft,
            Now.AddHours(1));

        var result = await store.QueryAsync(
            new UserIdentifierQuery
            {
                UserKey = user,
                IncludeDeleted = true
            });

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task QueryAsync_AppliesPagination()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        for (var i = 0; i < 5; i++)
        {
            await store.AddAsync(
                CreateIdentifier(
                    TenantA,
                    user,
                    value: $"user{i}@example.com",
                    normalizedValue: $"USER{i}@EXAMPLE.COM",
                    createdAt: Now.AddMinutes(i)));
        }

        var result = await store.QueryAsync(
            new UserIdentifierQuery
            {
                UserKey = user,
                PageNumber = 2,
                PageSize = 2,
                SortBy = nameof(UserIdentifier.CreatedAt)
            });

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);

        result.Items[0].CreatedAt.Should().Be(Now.AddMinutes(2));
        result.Items[1].CreatedAt.Should().Be(Now.AddMinutes(3));
    }

    [Fact]
    public async Task QueryAsync_SortByNormalizedValue_IsSupported()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        await store.AddAsync(CreateIdentifier(
            TenantA, user,
            value: "c@example.com",
            normalizedValue: "C@EXAMPLE.COM"));

        await store.AddAsync(CreateIdentifier(
            TenantA, user,
            value: "a@example.com",
            normalizedValue: "A@EXAMPLE.COM"));

        await store.AddAsync(CreateIdentifier(
            TenantA, user,
            value: "b@example.com",
            normalizedValue: "B@EXAMPLE.COM"));

        var result = await store.QueryAsync(
            new UserIdentifierQuery
            {
                UserKey = user,
                SortBy = nameof(UserIdentifier.NormalizedValue)
            });

        result.Items.Select(x => x.NormalizedValue)
            .Should()
            .ContainInOrder(
                "A@EXAMPLE.COM",
                "B@EXAMPLE.COM",
                "C@EXAMPLE.COM");
    }
}
