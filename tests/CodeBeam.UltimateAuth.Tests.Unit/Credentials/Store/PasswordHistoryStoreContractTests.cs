using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials.Contracts;

public abstract class PasswordHistoryStoreContractTests
{
    protected abstract Task<IPasswordHistoryStoreTestDatabase> CreateDatabaseAsync();

    private static readonly TenantKey TenantA =
        TenantKey.FromExternal("tenant-a");

    private static readonly TenantKey TenantB =
        TenantKey.FromExternal("tenant-b");

    private static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static CredentialKey Key(TenantKey tenant) =>
        new(tenant, Guid.NewGuid());

    private static PasswordHistoryEntry Entry(
        CredentialKey key,
        UserKey user,
        int minutes = 0,
        Guid? id = null) => new()
        {
            Id = id ?? Guid.NewGuid(),
            Tenant = key.Tenant,
            UserKey = user,
            CredentialKey = key,
            SecretHash = PasswordHash.Create(
            "test", $"hash-{Guid.NewGuid():N}"),
            ReplacedAt = Now.AddMinutes(minutes)
        };

    [Fact]
    public async Task AddAsync_PersistsAllFields()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);
        var key = Key(TenantA);
        var entry = Entry(key, UserKey.New());

        await store.AddAsync(entry, ct: TestContext.Current.CancellationToken);

        var stored = (await store.GetRecentAsync(key, 10, ct: TestContext.Current.CancellationToken))
            .Should().ContainSingle().Subject;

        stored.Id.Should().Be(entry.Id);
        stored.Tenant.Should().Be(entry.Tenant);
        stored.UserKey.Should().Be(entry.UserKey);
        stored.CredentialKey.Should().Be(entry.CredentialKey);
        stored.SecretHash.Should().Be(entry.SecretHash);
        stored.ReplacedAt.Should().Be(entry.ReplacedAt);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsNewestFirst()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);
        var key = Key(TenantA);
        var user = UserKey.New();

        var oldest = Entry(key, user, 1);
        var newest = Entry(key, user, 3);
        var middle = Entry(key, user, 2);

        await store.AddAsync(newest, ct: TestContext.Current.CancellationToken);
        await store.AddAsync(oldest, ct: TestContext.Current.CancellationToken);
        await store.AddAsync(middle, ct: TestContext.Current.CancellationToken);

        var result = await store.GetRecentAsync(key, 2, ct: TestContext.Current.CancellationToken);

        result.Select(x => x.Id)
            .Should().Equal(newest.Id, middle.Id);
    }

    [Fact]
    public async Task GetRecentAsync_EqualDates_UsesIdDescending()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);
        var key = Key(TenantA);
        var user = UserKey.New();

        var low = Entry(key, user, id: Guid.Parse(
            "00000000-0000-0000-0000-000000000001"));
        var high = Entry(key, user, id: Guid.Parse(
            "00000000-0000-0000-0000-000000000002"));

        await store.AddAsync(low, ct: TestContext.Current.CancellationToken);
        await store.AddAsync(high, ct: TestContext.Current.CancellationToken);

        var result = await store.GetRecentAsync(key, 2, ct: TestContext.Current.CancellationToken);

        result.Select(x => x.Id)
            .Should().Equal(high.Id, low.Id);
    }

    [Fact]
    public async Task GetRecentAsync_Zero_ReturnsEmpty()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);
        var key = Key(TenantA);

        await store.AddAsync(Entry(key, UserKey.New()), ct: TestContext.Current.CancellationToken);

        (await store.GetRecentAsync(key, 0, ct: TestContext.Current.CancellationToken))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentAsync_CountGreaterThanAvailable_ReturnsAll()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);
        var key = Key(TenantA);

        await store.AddAsync(Entry(key, UserKey.New()), ct: TestContext.Current.CancellationToken);

        (await store.GetRecentAsync(key, 100, ct: TestContext.Current.CancellationToken))
            .Should().ContainSingle();
    }

    [Fact]
    public async Task GetRecentAsync_NegativeCount_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var act = () => store.GetRecentAsync(Key(TenantA), -1);

        await act.Should()
            .ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetRecentAsync_MissingCredential_ReturnsEmpty()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        (await store.GetRecentAsync(Key(TenantA), 5, ct: TestContext.Current.CancellationToken))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentAsync_DoesNotMixCredentials()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);
        var keyA = Key(TenantA);
        var keyB = Key(TenantA);
        var user = UserKey.New();

        var a = Entry(keyA, user);
        await store.AddAsync(a, ct: TestContext.Current.CancellationToken);
        await store.AddAsync(Entry(keyB, user), ct: TestContext.Current.CancellationToken);

        var result = await store.GetRecentAsync(keyA, 10, ct: TestContext.Current.CancellationToken);

        result.Should().ContainSingle();
        result.Single().Id.Should().Be(a.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public async Task PruneAsync_KeepsExactlyNewestEntries(
        int keepLast)
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);
        var key = Key(TenantA);
        var user = UserKey.New();

        var entries = Enumerable.Range(0, 5)
            .Select(i => Entry(key, user, i))
            .ToArray();

        foreach (var entry in entries.Reverse())
            await store.AddAsync(entry, ct: TestContext.Current.CancellationToken);

        await store.PruneAsync(key, keepLast, ct: TestContext.Current.CancellationToken);

        var result = await store.GetRecentAsync(key, 20, ct: TestContext.Current.CancellationToken);
        var expected = entries
            .OrderByDescending(x => x.ReplacedAt)
            .Take(keepLast)
            .Select(x => x.Id);

        result.Select(x => x.Id).Should().Equal(expected);
    }

    [Fact]
    public async Task PruneAsync_DoesNotAffectOtherCredentials()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);
        var keyA = Key(TenantA);
        var keyB = Key(TenantA);
        var user = UserKey.New();

        await store.AddAsync(Entry(keyA, user), ct: TestContext.Current.CancellationToken);
        var other = Entry(keyB, user);
        await store.AddAsync(other, ct: TestContext.Current.CancellationToken);

        await store.PruneAsync(keyA, 0, ct: TestContext.Current.CancellationToken);

        (await store.GetRecentAsync(keyA, 10, ct: TestContext.Current.CancellationToken)).Should().BeEmpty();
        (await store.GetRecentAsync(keyB, 10, ct: TestContext.Current.CancellationToken))
            .Should().ContainSingle(x => x.Id == other.Id);
    }

    [Fact]
    public async Task PruneAsync_NegativeCount_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var act = () => store.PruneAsync(Key(TenantA), -1);

        await act.Should()
            .ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task PruneAsync_IsIdempotent()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);
        var key = Key(TenantA);
        var user = UserKey.New();

        await store.AddAsync(Entry(key, user, 1), ct: TestContext.Current.CancellationToken);
        await store.AddAsync(Entry(key, user, 2), ct: TestContext.Current.CancellationToken);

        await store.PruneAsync(key, 1, ct: TestContext.Current.CancellationToken);
        var first = await store.GetRecentAsync(key, 10, ct: TestContext.Current.CancellationToken);

        await store.PruneAsync(key, 1, ct: TestContext.Current.CancellationToken);
        var second = await store.GetRecentAsync(key, 10, ct: TestContext.Current.CancellationToken);

        second.Should().BeEquivalentTo(first);
    }

    [Fact]
    public async Task DeleteByCredentialAsync_RemovesOnlyTarget()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);
        var keyA = Key(TenantA);
        var keyB = Key(TenantA);
        var user = UserKey.New();

        await store.AddAsync(Entry(keyA, user), ct: TestContext.Current.CancellationToken);
        await store.AddAsync(Entry(keyA, user), ct: TestContext.Current.CancellationToken);
        var other = Entry(keyB, user);
        await store.AddAsync(other, ct: TestContext.Current.CancellationToken);

        await store.DeleteByCredentialAsync(keyA, ct: TestContext.Current.CancellationToken);

        (await store.GetRecentAsync(keyA, 10, ct: TestContext.Current.CancellationToken)).Should().BeEmpty();
        (await store.GetRecentAsync(keyB, 10, ct: TestContext.Current.CancellationToken))
            .Should().ContainSingle(x => x.Id == other.Id);
    }

    [Fact]
    public async Task DeleteByCredentialAsync_Missing_IsIdempotent()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var key = Key(TenantA);

        await store.DeleteByCredentialAsync(key, ct: TestContext.Current.CancellationToken);
        await store.DeleteByCredentialAsync(key, ct: TestContext.Current.CancellationToken);

        (await store.GetRecentAsync(key, 10, ct: TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteByUserAsync_RemovesAllUsersCredentials()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);
        var userA = UserKey.New();
        var userB = UserKey.New();
        var keyA1 = Key(TenantA);
        var keyA2 = Key(TenantA);
        var keyB = Key(TenantA);

        await store.AddAsync(Entry(keyA1, userA), ct: TestContext.Current.CancellationToken);
        await store.AddAsync(Entry(keyA2, userA), ct: TestContext.Current.CancellationToken);
        var other = Entry(keyB, userB);
        await store.AddAsync(other, ct: TestContext.Current.CancellationToken);

        await store.DeleteByUserAsync(userA, ct: TestContext.Current.CancellationToken);

        (await store.GetRecentAsync(keyA1, 10, ct: TestContext.Current.CancellationToken)).Should().BeEmpty();
        (await store.GetRecentAsync(keyA2, 10, ct: TestContext.Current.CancellationToken)).Should().BeEmpty();
        (await store.GetRecentAsync(keyB, 10, ct: TestContext.Current.CancellationToken))
            .Should().ContainSingle(x => x.Id == other.Id);
    }

    [Fact]
    public async Task DeleteByUserAsync_Missing_IsIdempotent()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var user = UserKey.New();

        await store.DeleteByUserAsync(user, ct: TestContext.Current.CancellationToken);
        await store.DeleteByUserAsync(user, ct: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TenantIsolation_GetRecentAndPrune()
    {
        await using var db = await CreateDatabaseAsync();
        var storeA = db.CreateStore(TenantA);
        var storeB = db.CreateStore(TenantB);
        var user = UserKey.New();
        var credentialId = Guid.NewGuid();

        var keyA = new CredentialKey(TenantA, credentialId);
        var keyB = new CredentialKey(TenantB, credentialId);

        await storeA.AddAsync(Entry(keyA, user), ct: TestContext.Current.CancellationToken);
        await storeB.AddAsync(Entry(keyB, user), ct: TestContext.Current.CancellationToken);

        (await storeA.GetRecentAsync(keyA, 10, ct: TestContext.Current.CancellationToken))
            .Should().ContainSingle();
        (await storeB.GetRecentAsync(keyB, 10, ct: TestContext.Current.CancellationToken))
            .Should().ContainSingle();

        await storeA.PruneAsync(keyA, 0, ct: TestContext.Current.CancellationToken);

        (await storeA.GetRecentAsync(keyA, 10, ct: TestContext.Current.CancellationToken)).Should().BeEmpty();
        (await storeB.GetRecentAsync(keyB, 10, ct: TestContext.Current.CancellationToken))
            .Should().ContainSingle();
    }

    [Fact]
    public async Task TenantIsolation_DeleteByUser()
    {
        await using var db = await CreateDatabaseAsync();
        var storeA = db.CreateStore(TenantA);
        var storeB = db.CreateStore(TenantB);
        var user = UserKey.New();
        var credentialId = Guid.NewGuid();

        var keyA = new CredentialKey(TenantA, credentialId);
        var keyB = new CredentialKey(TenantB, credentialId);

        await storeA.AddAsync(Entry(keyA, user), ct: TestContext.Current.CancellationToken);
        await storeB.AddAsync(Entry(keyB, user), ct: TestContext.Current.CancellationToken);

        await storeA.DeleteByUserAsync(user, ct: TestContext.Current.CancellationToken);

        (await storeA.GetRecentAsync(keyA, 10, ct: TestContext.Current.CancellationToken)).Should().BeEmpty();
        (await storeB.GetRecentAsync(keyB, 10, ct: TestContext.Current.CancellationToken))
            .Should().ContainSingle();
    }

    [Fact]
    public async Task AddAsync_WrongTenant_ThrowsConflict()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var act = () => store.AddAsync(
            Entry(Key(TenantB), UserKey.New()));

        var exception = await act.Should()
            .ThrowAsync<UAuthConflictException>();

        exception.Which.Code.Should().Be("tenant_mismatch");
    }

    [Fact]
    public async Task GetRecentAsync_WrongTenant_ThrowsConflict()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var act = () => store.GetRecentAsync(Key(TenantB), 5);

        await act.Should().ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task PruneAsync_WrongTenant_ThrowsConflict()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var act = () => store.PruneAsync(Key(TenantB), 1);

        await act.Should().ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task DeleteByCredentialAsync_WrongTenant_ThrowsConflict()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var act = () => store.DeleteByCredentialAsync(Key(TenantB));

        await act.Should().ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task AddAsync_EmptyId_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var entry = Entry(Key(TenantA), UserKey.New())
            with
        { Id = Guid.Empty };

        var act = () => store.AddAsync(entry);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddAsync_NullEntry_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        var act = () => store.AddAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddAsync_DuplicateId_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);
        var key = Key(TenantA);
        var entry = Entry(key, UserKey.New());

        await store.AddAsync(entry, ct: TestContext.Current.CancellationToken);

        var act = () => store.AddAsync(entry);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetRecentAsync_Cancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.GetRecentAsync(
            Key(TenantA), 5, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AddAsync_Cancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.AddAsync(
            Entry(Key(TenantA), UserKey.New()), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task PruneAsync_Cancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.PruneAsync(
            Key(TenantA), 1, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DeleteByCredentialAsync_Cancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.DeleteByCredentialAsync(
            Key(TenantA), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DeleteByUserAsync_Cancelled_Throws()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(TenantA);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => store.DeleteByUserAsync(
            UserKey.New(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
