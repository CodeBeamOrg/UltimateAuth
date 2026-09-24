using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;

public abstract class UserProfileStoreContractTests
{
    protected abstract Task<IUserProfileStoreTestDatabase> CreateDatabaseAsync();

    protected static readonly TenantKey TenantA =
        TenantKey.FromExternal("tenant-a");

    protected static readonly TenantKey TenantB =
        TenantKey.FromExternal("tenant-b");

    protected static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    protected static UserProfile CreateProfile(
        TenantKey tenant,
        UserKey? userKey = null,
        ProfileKey? profileKey = null,
        DateTimeOffset? createdAt = null,
        string? firstName = "Alice",
        string? lastName = "Example",
        string? displayName = "Alice Example")
    {
        return UserProfile.Create(
            id: null,
            tenant: tenant,
            userKey: userKey ?? UserKey.New(),
            profileKey: profileKey,
            createdAt: createdAt ?? Now,
            firstName: firstName,
            lastName: lastName,
            displayName: displayName,
            birthDate: null,
            gender: null,
            bio: null,
            language: null,
            timezone: null,
            culture: null);
    }

    // ============================================================
    // ADD / GET / EXISTS
    // ============================================================

    [Fact]
    public async Task AddAsync_PersistsProfile()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var profile = CreateProfile(TenantA);

        await store.AddAsync(profile);

        var key = new UserProfileKey(
            TenantA,
            profile.UserKey,
            profile.ProfileKey);

        var persisted = await store.GetAsync(key);

        persisted.Should().NotBeNull();
        persisted!.Id.Should().Be(profile.Id);
        persisted.Tenant.Should().Be(TenantA);
        persisted.UserKey.Should().Be(profile.UserKey);
        persisted.ProfileKey.Should().Be(profile.ProfileKey);
        persisted.FirstName.Should().Be("Alice");
        persisted.LastName.Should().Be("Example");
        persisted.DisplayName.Should().Be("Alice Example");
        persisted.CreatedAt.Should().Be(Now);
        persisted.Version.Should().Be(0);
        persisted.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_WhenMissing_ReturnsNull()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var result = await store.GetAsync(
            new UserProfileKey(
                TenantA,
                UserKey.New(),
                ProfileKey.Default));

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenPresent_ReturnsTrue()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var profile = CreateProfile(TenantA);
        await store.AddAsync(profile);

        var result = await store.ExistsAsync(
            new UserProfileKey(
                TenantA,
                profile.UserKey,
                profile.ProfileKey));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenMissing_ReturnsFalse()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var result = await store.ExistsAsync(
            new UserProfileKey(
                TenantA,
                UserKey.New(),
                ProfileKey.Default));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_WhenSameUserAndProfileKeyAlreadyExists_ThrowsConflict()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        await store.AddAsync(
            CreateProfile(TenantA, user));

        var duplicate = CreateProfile(
            TenantA,
            user,
            ProfileKey.Default);

        var act = () => store.AddAsync(duplicate);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task SameUser_CanHaveDifferentProfileKeys()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var defaultProfile = CreateProfile(
            TenantA,
            user,
            ProfileKey.Default);

        var secondaryKey = ProfileKey.Parse("work", null);

        var workProfile = CreateProfile(
            TenantA,
            user,
            secondaryKey);

        await store.AddAsync(defaultProfile);
        await store.AddAsync(workProfile);

        var profiles = await store.GetAllProfilesByUserAsync(user);

        profiles.Should().HaveCount(2);
        profiles.Select(x => x.ProfileKey)
            .Should()
            .BeEquivalentTo([ProfileKey.Default, secondaryKey]);
    }

    [Fact]
    public async Task AddAsync_WhenVersionIsNotZero_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var profile = CreateProfile(TenantA);
        profile.Version = 42;

        var act = () => store.AddAsync(profile);

        await act.Should()
            .ThrowAsync<InvalidOperationException>();
    }

    // ============================================================
    // SAVE
    // ============================================================

    [Fact]
    public async Task SaveAsync_PersistsAllMutableFields_AndIncrementsVersion()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var profile = CreateProfile(TenantA);

        await store.AddAsync(profile);

        var changedAt = Now.AddMinutes(10);

        profile.UpdateName(
            "Bob",
            "Changed",
            "Bob Changed",
            changedAt);

        profile.UpdatePersonalInfo(
            new DateOnly(1990, 5, 10),
            "male",
            "Updated bio",
            changedAt);

        profile.UpdateLocalization(
            "tr",
            "Europe/Istanbul",
            "tr-TR",
            changedAt);

        profile.UpdateMetadata(
            new Dictionary<string, string>
            {
                ["department"] = "engineering",
                ["region"] = "emea"
            },
            changedAt);

        await store.SaveAsync(
            profile,
            expectedVersion: 0);

        var persisted = await store.GetAsync(
            new UserProfileKey(
                TenantA,
                profile.UserKey,
                profile.ProfileKey));

        persisted.Should().NotBeNull();

        persisted!.FirstName.Should().Be("Bob");
        persisted.LastName.Should().Be("Changed");
        persisted.DisplayName.Should().Be("Bob Changed");

        persisted.BirthDate.Should().Be(new DateOnly(1990, 5, 10));
        persisted.Gender.Should().Be("male");
        persisted.Bio.Should().Be("Updated bio");

        persisted.Language.Should().Be("tr");
        persisted.TimeZone.Should().Be("Europe/Istanbul");
        persisted.Culture.Should().Be("tr-TR");

        persisted.Metadata.Should().NotBeNull();
        persisted.Metadata!["department"].Should().Be("engineering");
        persisted.Metadata["region"].Should().Be("emea");

        persisted.UpdatedAt.Should().Be(changedAt);
        persisted.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_WhenMissing_ThrowsNotFound()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var profile = CreateProfile(TenantA);

        var act = () => store.SaveAsync(
            profile,
            expectedVersion: 0);

        await act.Should()
            .ThrowAsync<UAuthNotFoundException>();
    }

    [Fact]
    public async Task SaveAsync_WhenVersionIsStale_ThrowsConcurrency()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var profile = CreateProfile(TenantA);
        await store.AddAsync(profile);

        profile.UpdateName(
            "First",
            "Update",
            "First Update",
            Now.AddMinutes(1));

        await store.SaveAsync(profile, 0);

        profile.UpdateName(
            "Second",
            "Update",
            "Second Update",
            Now.AddMinutes(2));

        var act = () => store.SaveAsync(
            profile,
            expectedVersion: 0);

        await act.Should()
            .ThrowAsync<UAuthConcurrencyException>();
    }

    // ============================================================
    // DELETE
    // ============================================================

    [Fact]
    public async Task DeleteAsync_Soft_PreservesProfileAndDeletionState()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var profile = CreateProfile(TenantA);
        await store.AddAsync(profile);

        var deletedAt = Now.AddHours(1);

        await store.DeleteAsync(
            new UserProfileKey(
                TenantA,
                profile.UserKey,
                profile.ProfileKey),
            expectedVersion: 0,
            DeleteMode.Soft,
            deletedAt);

        var persisted = await store.GetAsync(
            new UserProfileKey(
                TenantA,
                profile.UserKey,
                profile.ProfileKey));

        persisted.Should().NotBeNull();
        persisted!.IsDeleted.Should().BeTrue();
        persisted.DeletedAt.Should().Be(deletedAt);
        persisted.Version.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_Hard_RemovesProfile()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var profile = CreateProfile(TenantA);
        var key = new UserProfileKey(
            TenantA,
            profile.UserKey,
            profile.ProfileKey);

        await store.AddAsync(profile);

        await store.DeleteAsync(
            key,
            0,
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
            new UserProfileKey(
                TenantA,
                UserKey.New(),
                ProfileKey.Default),
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

        var profile = CreateProfile(TenantA);
        var key = new UserProfileKey(
            TenantA,
            profile.UserKey,
            profile.ProfileKey);

        await store.AddAsync(profile);

        profile.UpdateName(
            "Changed",
            null,
            null,
            Now.AddMinutes(1));

        await store.SaveAsync(profile, 0);

        var act = () => store.DeleteAsync(
            key,
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
        var profile = CreateProfile(TenantB);

        var act = () => store.AddAsync(profile);

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task SaveAsync_WhenEntityBelongsToDifferentTenant_IsRejected()
    {
        await using var db = await CreateDatabaseAsync();

        var storeA = db.CreateStore(TenantA);
        var storeB = db.CreateStore(TenantB);

        var profile = CreateProfile(TenantB);
        await storeB.AddAsync(profile);

        var act = () => storeA.SaveAsync(
            profile,
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

        await storeA.AddAsync(CreateProfile(TenantA));
        await storeB.AddAsync(CreateProfile(TenantB));

        var resultA = await storeA.QueryAsync(
            new UserProfileQuery());

        var resultB = await storeB.QueryAsync(
            new UserProfileQuery());

        resultA.Items.Should().ContainSingle();
        resultA.Items.Single().Tenant.Should().Be(TenantA);

        resultB.Items.Should().ContainSingle();
        resultB.Items.Single().Tenant.Should().Be(TenantB);
    }

    // ============================================================
    // GET ALL / GET BY USERS
    // ============================================================

    [Fact]
    public async Task GetAllProfilesByUserAsync_ReturnsActiveProfilesOnly()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        var first = CreateProfile(
            TenantA,
            user,
            ProfileKey.Default);

        var secondaryKey = ProfileKey.Parse("work", null);

        var second = CreateProfile(
            TenantA,
            user,
            secondaryKey);

        await store.AddAsync(first);
        await store.AddAsync(second);

        await store.DeleteAsync(
            new UserProfileKey(
                TenantA,
                user,
                secondaryKey),
            0,
            DeleteMode.Soft,
            Now.AddHours(1));

        var result =
            await store.GetAllProfilesByUserAsync(user);

        result.Should().ContainSingle();
        result.Single().ProfileKey.Should().Be(ProfileKey.Default);
    }

    [Fact]
    public async Task GetAllProfilesByUserAsync_DoesNotReturnOtherUsersProfiles()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var userA = UserKey.New();
        var userB = UserKey.New();

        await store.AddAsync(CreateProfile(TenantA, userA));
        await store.AddAsync(CreateProfile(TenantA, userB));

        var result =
            await store.GetAllProfilesByUserAsync(userA);

        result.Should().ContainSingle();
        result.Single().UserKey.Should().Be(userA);
    }

    [Fact]
    public async Task GetByUsersAsync_ReturnsRequestedUsersForProfileKey()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var userA = UserKey.New();
        var userB = UserKey.New();
        var userC = UserKey.New();

        await store.AddAsync(CreateProfile(TenantA, userA));
        await store.AddAsync(CreateProfile(TenantA, userB));
        await store.AddAsync(CreateProfile(TenantA, userC));

        var result = await store.GetByUsersAsync(
            [userA, userB],
            ProfileKey.Default);

        result.Should().HaveCount(2);

        result.Select(x => x.UserKey)
            .Should()
            .BeEquivalentTo([userA, userB]);
    }

    [Fact]
    public async Task GetByUsersAsync_ExcludesDeletedProfiles()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var userA = UserKey.New();
        var userB = UserKey.New();

        var first = CreateProfile(TenantA, userA);
        var second = CreateProfile(TenantA, userB);

        await store.AddAsync(first);
        await store.AddAsync(second);

        await store.DeleteAsync(
            new UserProfileKey(
                TenantA,
                userB,
                ProfileKey.Default),
            0,
            DeleteMode.Soft,
            Now.AddHours(1));

        var result = await store.GetByUsersAsync(
            [userA, userB],
            ProfileKey.Default);

        result.Should().ContainSingle();
        result.Single().UserKey.Should().Be(userA);
    }

    // ============================================================
    // QUERY
    // ============================================================

    [Fact]
    public async Task QueryAsync_ByDefault_ExcludesDeleted()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var active = CreateProfile(TenantA);
        var deleted = CreateProfile(TenantA);

        await store.AddAsync(active);
        await store.AddAsync(deleted);

        await store.DeleteAsync(
            new UserProfileKey(
                TenantA,
                deleted.UserKey,
                deleted.ProfileKey),
            0,
            DeleteMode.Soft,
            Now.AddHours(1));

        var result = await store.QueryAsync(
            new UserProfileQuery());

        result.Items.Should().ContainSingle();
        result.Items.Single().Id.Should().Be(active.Id);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_IncludeDeleted_ReturnsDeletedProfiles()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var active = CreateProfile(TenantA);
        var deleted = CreateProfile(TenantA);

        await store.AddAsync(active);
        await store.AddAsync(deleted);

        await store.DeleteAsync(
            new UserProfileKey(
                TenantA,
                deleted.UserKey,
                deleted.ProfileKey),
            0,
            DeleteMode.Soft,
            Now.AddHours(1));

        var result = await store.QueryAsync(
            new UserProfileQuery
            {
                IncludeDeleted = true
            });

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().Contain(x =>
            x.Id == deleted.Id && x.IsDeleted);
    }

    [Fact]
    public async Task QueryAsync_ProfileKey_FiltersProfiles()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var workKey = ProfileKey.Parse("work", null);

        await store.AddAsync(
            CreateProfile(
                TenantA,
                profileKey: ProfileKey.Default));

        var work = CreateProfile(
            TenantA,
            profileKey: workKey);

        await store.AddAsync(work);

        var result = await store.QueryAsync(
            new UserProfileQuery
            {
                ProfileKey = workKey
            });

        result.Items.Should().ContainSingle();
        result.Items.Single().Id.Should().Be(work.Id);
    }

    [Fact]
    public async Task QueryAsync_AppliesPagination()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        for (var i = 0; i < 5; i++)
        {
            await store.AddAsync(
                CreateProfile(
                    TenantA,
                    createdAt: Now.AddMinutes(i),
                    displayName: $"User {i}"));
        }

        var result = await store.QueryAsync(
            new UserProfileQuery
            {
                PageNumber = 2,
                PageSize = 2,
                SortBy = nameof(UserProfile.CreatedAt)
            });

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);

        result.Items[0].CreatedAt
            .Should().Be(Now.AddMinutes(2));

        result.Items[1].CreatedAt
            .Should().Be(Now.AddMinutes(3));
    }

    [Theory]
    [InlineData(nameof(UserProfile.DisplayName))]
    [InlineData(nameof(UserProfile.FirstName))]
    [InlineData(nameof(UserProfile.LastName))]
    public async Task QueryAsync_SortsStringFieldsAscending(
        string sortBy)
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        await store.AddAsync(
            CreateProfile(
                TenantA,
                firstName: "Charlie",
                lastName: "Zulu",
                displayName: "Mike"));

        await store.AddAsync(
            CreateProfile(
                TenantA,
                firstName: "Alice",
                lastName: "Alpha",
                displayName: "Alpha"));

        await store.AddAsync(
            CreateProfile(
                TenantA,
                firstName: "Bob",
                lastName: "Mike",
                displayName: "Zulu"));

        var result = await store.QueryAsync(
            new UserProfileQuery
            {
                SortBy = sortBy
            });

        result.Items.Should().HaveCount(3);

        switch (sortBy)
        {
            case nameof(UserProfile.FirstName):
                result.Items.Select(x => x.FirstName)
                    .Should()
                    .ContainInOrder("Alice", "Bob", "Charlie");
                break;

            case nameof(UserProfile.LastName):
                result.Items.Select(x => x.LastName)
                    .Should()
                    .ContainInOrder("Alpha", "Mike", "Zulu");
                break;

            case nameof(UserProfile.DisplayName):
                result.Items.Select(x => x.DisplayName)
                    .Should()
                    .ContainInOrder("Alpha", "Mike", "Zulu");
                break;
        }
    }

    [Fact]
    public async Task QueryAsync_SortsCreatedAtDescending()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        await store.AddAsync(
            CreateProfile(
                TenantA,
                createdAt: Now.AddMinutes(1)));

        await store.AddAsync(
            CreateProfile(
                TenantA,
                createdAt: Now.AddMinutes(3)));

        await store.AddAsync(
            CreateProfile(
                TenantA,
                createdAt: Now.AddMinutes(2)));

        var result = await store.QueryAsync(
            new UserProfileQuery
            {
                SortBy = nameof(UserProfile.CreatedAt),
                Descending = true
            });

        result.Items.Select(x => x.CreatedAt)
            .Should()
            .ContainInOrder(
                Now.AddMinutes(3),
                Now.AddMinutes(2),
                Now.AddMinutes(1));
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
            new UserProfileKey(
                TenantA,
                UserKey.New(),
                ProfileKey.Default),
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
            CreateProfile(TenantA),
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
            new UserProfileQuery(),
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }
}
