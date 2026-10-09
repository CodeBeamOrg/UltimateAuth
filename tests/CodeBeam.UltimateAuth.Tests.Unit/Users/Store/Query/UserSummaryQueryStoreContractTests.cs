
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users.Contracts;

public abstract class UserSummaryQueryStoreContractTests
{
    protected abstract Task<IUserSummaryQueryStoreTestDatabase>
        CreateDatabaseAsync();

    protected static readonly TenantKey TenantA =
        TenantKey.FromExternal("tenant-a");

    protected static readonly TenantKey TenantB =
        TenantKey.FromExternal("tenant-b");

    protected static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task QueryAsync_WhenEmpty_ReturnsEmptyPage()
    {
        await using var db = await CreateDatabaseAsync();

        var store = db.CreateStore(TenantA);

        var result = await store.QueryAsync(new UserQuery());

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task QueryAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var sharedKey = UserKey.New();

        await db.SeedAsync(TenantA,
        [
            CreateUser(sharedKey, "Tenant A User")
        ]);

        await db.SeedAsync(TenantB,
        [
            CreateUser(sharedKey, "Tenant B User")
        ]);

        var resultA = await db.CreateStore(TenantA)
            .QueryAsync(new UserQuery());

        var resultB = await db.CreateStore(TenantB)
            .QueryAsync(new UserQuery());

        resultA.TotalCount.Should().Be(1);
        resultB.TotalCount.Should().Be(1);

        resultA.Items.Single().DisplayName
            .Should().Be("Tenant A User");

        resultB.Items.Single().DisplayName
            .Should().Be("Tenant B User");
    }

    [Fact]
    public async Task QueryAsync_With1501Users_ReturnsCorrectLastPage()
    {
        await using var db = await CreateDatabaseAsync();

        var users = Enumerable.Range(1, 1501)
            .Select(i => CreateUser(
                UserKey.New(),
                $"User {i:D4}",
                Now.AddSeconds(i)))
            .ToArray();

        await db.SeedAsync(TenantA, users);

        var result = await db.CreateStore(TenantA)
            .QueryAsync(new UserQuery
            {
                PageNumber = 7,
                PageSize = 250
            });

        result.TotalCount.Should().Be(1501);
        result.Items.Should().ContainSingle();
        result.Items[0].UserKey.Should().Be(users[1500].UserKey);
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task QueryAsync_SearchesBeyondFirst1000Users()
    {
        await using var db = await CreateDatabaseAsync();

        var users = Enumerable.Range(1, 1501)
            .Select(i => CreateUser(
                UserKey.New(),
                $"User {i:D4}",
                Now.AddSeconds(i)) with
            {
                Email = i == 1501
                        ? "target@example.com"
                        : $"user{i}@example.com"
            })
            .ToArray();

        await db.SeedAsync(TenantA, users);

        var result = await db.CreateStore(TenantA)
            .QueryAsync(new UserQuery
            {
                Search = "target@example.com",
                PageSize = 50
            });

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].UserKey.Should().Be(users[1500].UserKey);
    }


    [Theory]
    [InlineData("display")]
    [InlineData("USERNAME")]
    [InlineData("EXAMPLE.COM")]
    [InlineData("555123")]
    public async Task QueryAsync_SearchesSupportedFields(
        string search)
    {
        await using var db = await CreateDatabaseAsync();

        var target = new UserSummarySeed
        {
            UserKey = UserKey.New(),
            CreatedAt = Now,
            Status = UserStatus.Active,
            DisplayName = "Target Display",
            UserName = "targetusername",
            Email = "target@example.com",
            Phone = "5551234567"
        };

        await db.SeedAsync(TenantA, [target]);

        var result = await db.CreateStore(TenantA)
            .QueryAsync(new UserQuery { Search = search });

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].UserKey.Should().Be(target.UserKey);
    }

    [Fact]
    public async Task QueryAsync_SearchWithNoMatches_ReturnsEmpty()
    {
        await using var db = await CreateDatabaseAsync();

        await db.SeedAsync(TenantA,
        [
            CreateUser(UserKey.New(), "Alice"),
        CreateUser(UserKey.New(), "Bob")
        ]);

        var result = await db.CreateStore(TenantA)
            .QueryAsync(new UserQuery
            {
                Search = "does-not-exist"
            });

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task QueryAsync_EmptySearchDoesNotFilter(
        string? search)
    {
        await using var db = await CreateDatabaseAsync();

        await db.SeedAsync(TenantA,
        [
            CreateUser(UserKey.New(), "Alice"),
        CreateUser(UserKey.New(), "Bob")
        ]);

        var result = await db.CreateStore(TenantA)
            .QueryAsync(new UserQuery { Search = search });

        result.TotalCount.Should().Be(2);
    }


    [Fact]
    public async Task QueryAsync_PaginationHasNoMissingOrDuplicateUsers()
    {
        await using var db = await CreateDatabaseAsync();

        var users = Enumerable.Range(1, 105)
            .Select(i => CreateUser(
                UserKey.New(),
                $"User {i:D3}",
                Now))
            .ToArray();

        await db.SeedAsync(TenantA, users);

        var store = db.CreateStore(TenantA);
        var collected = new List<UserKey>();

        for (var page = 1; page <= 11; page++)
        {
            var result = await store.QueryAsync(new UserQuery
            {
                PageNumber = page,
                PageSize = 10
            });

            result.TotalCount.Should().Be(105);
            collected.AddRange(result.Items.Select(x => x.UserKey));
        }

        collected.Should().HaveCount(105);
        collected.Distinct().Should().HaveCount(105);

        collected.Should().BeEquivalentTo(
            users.Select(x => x.UserKey));
    }

    [Fact]
    public async Task QueryAsync_PageBeyondLast_ReturnsEmptyButCorrectTotal()
    {
        await using var db = await CreateDatabaseAsync();

        var users = Enumerable.Range(1, 15)
            .Select(i => CreateUser(
                UserKey.New(), $"User {i}"))
            .ToArray();

        await db.SeedAsync(TenantA, users);

        var result = await db.CreateStore(TenantA)
            .QueryAsync(new UserQuery
            {
                PageNumber = 10,
                PageSize = 10
            });

        result.TotalCount.Should().Be(15);
        result.Items.Should().BeEmpty();
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task QueryAsync_ExtremePageNumber_DoesNotOverflow()
    {
        await using var db = await CreateDatabaseAsync();

        await db.SeedAsync(TenantA,
        [
            CreateUser(UserKey.New(), "Alice")
        ]);

        var result = await db.CreateStore(TenantA)
            .QueryAsync(new UserQuery
            {
                PageNumber = int.MaxValue,
                PageSize = 1000
            });

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(1);
        result.HasNext.Should().BeFalse();
    }


    [Fact]
    public async Task QueryAsync_FiltersByStatus()
    {
        await using var db = await CreateDatabaseAsync();

        var active = CreateUser(
            UserKey.New(), "Active User");

        var other = CreateUser(
            UserKey.New(), "Other User") with
        {
            Status = UserStatus.Suspended
        };

        await db.SeedAsync(TenantA, [active, other]);

        var result = await db.CreateStore(TenantA)
            .QueryAsync(new UserQuery
            {
                Status = UserStatus.Active
            });

        result.TotalCount.Should().Be(1);
        result.Items.Single().UserKey.Should().Be(active.UserKey);
    }

    [Fact]
    public async Task QueryAsync_ExcludesDeletedUsersByDefault()
    {
        await using var db = await CreateDatabaseAsync();

        var active = CreateUser(
            UserKey.New(), "Active");

        var deleted = CreateUser(
            UserKey.New(), "Deleted") with
        {
            IsDeleted = true
        };

        await db.SeedAsync(TenantA, [active, deleted]);

        var store = db.CreateStore(TenantA);

        var normal = await store.QueryAsync(new UserQuery());
        var includingDeleted = await store.QueryAsync(
            new UserQuery { IncludeDeleted = true });

        normal.TotalCount.Should().Be(1);
        normal.Items.Single().UserKey.Should().Be(active.UserKey);

        includingDeleted.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task QueryAsync_UserWithoutProfileIsStillReturned()
    {
        await using var db = await CreateDatabaseAsync();

        var user = CreateUser(
            UserKey.New(), "Not Persisted") with
        {
            HasProfile = false
        };

        await db.SeedAsync(TenantA, [user]);

        var result = await db.CreateStore(TenantA)
            .QueryAsync(new UserQuery());

        result.Items.Should().ContainSingle();
        result.Items[0].UserKey.Should().Be(user.UserKey);
        result.Items[0].DisplayName.Should().BeNull();
    }


    [Fact]
    public async Task QueryAsync_UsesRequestedProfileKey()
    {
        await using var db = await CreateDatabaseAsync();

        var userKey = UserKey.New();
        var customProfile = ProfileKey.Parse("business", null);

        await db.SeedAsync(TenantA,
        [
            CreateUser(userKey, "Business Profile") with
        {
            ProfileKey = customProfile
        }
        ]);

        var store = db.CreateStore(TenantA);

        var defaultResult = await store.QueryAsync(
            new UserQuery());

        var businessResult = await store.QueryAsync(
            new UserQuery
            {
                ProfileKey = customProfile
            });

        defaultResult.TotalCount.Should().Be(1);
        defaultResult.Items.Single().DisplayName.Should().BeNull();

        businessResult.TotalCount.Should().Be(1);
        businessResult.Items.Single().DisplayName
            .Should().Be("Business Profile");
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    public async Task QueryAsync_SearchTreatsSqlWildcardsLiterally(
        string search)
    {
        await using var db = await CreateDatabaseAsync();

        var target = CreateUser(
            UserKey.New(), $"Name{search}Value");

        var other = CreateUser(
            UserKey.New(), "NameWithoutWildcard");

        await db.SeedAsync(TenantA, [target, other]);

        var result = await db.CreateStore(TenantA)
            .QueryAsync(new UserQuery
            {
                Search = search
            });

        result.TotalCount.Should().Be(1);
        result.Items.Single().UserKey.Should().Be(target.UserKey);
    }

    [Fact]
    public async Task QueryAsync_WhenCancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var store = db.CreateStore(TenantA);

        var act = () => store.QueryAsync(
            new UserQuery(),
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }


    [Fact]
    public async Task QueryAsync_SearchesByExactUserKey()
    {
        await using var db = await CreateDatabaseAsync();

        var key = UserKey.FromString("specific-user-key-12345");

        await db.SeedAsync(TenantA,
        [
            CreateUser(key, "Target User"),
        CreateUser(UserKey.New(), "Other User")
        ]);

        var result = await db.CreateStore(TenantA)
            .QueryAsync(new UserQuery
            {
                Search = key.Value
            });

        result.TotalCount.Should().Be(1);
        result.Items.Single().UserKey.Should().Be(key);
    }

    [Fact]
    public async Task QueryAsync_DoesNotMatchPartialUserKey()
    {
        await using var db = await CreateDatabaseAsync();

        var key = UserKey.FromString("specific-user-key-12345");

        await db.SeedAsync(TenantA,
        [
            CreateUser(key, "Target User")
        ]);

        var result = await db.CreateStore(TenantA)
            .QueryAsync(new UserQuery
            {
                Search = "user-key-12345"
            });

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }


    private static UserSummarySeed CreateUser(
        UserKey key,
        string displayName,
        DateTimeOffset? createdAt = null)
    {
        return new UserSummarySeed
        {
            UserKey = key,
            DisplayName = displayName,
            CreatedAt = createdAt ?? Now
        };
    }
}
