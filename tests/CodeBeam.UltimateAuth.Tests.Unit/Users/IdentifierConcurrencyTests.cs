using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.InMemory;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class IdentifierConcurrencyTests
{
    [Fact]
    public async Task Save_should_increment_version()
    {
        var store = new InMemoryUserIdentifierStore(new TenantContext(TenantKeys.Single));
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        var identifier = UserIdentifier.Create(id, TenantKey.Single, TestUsers.Admin, UserIdentifierType.Email, "a@test.com", "a@test.com", now);
        await store.AddAsync(identifier);

        var copy = await store.GetByIdAsync(id);
        var expected = copy!.Version;

        copy.ChangeValue("b@test.com", "b@test.com", now);
        await store.SaveAsync(copy, expected);

        var updated = await store.GetByIdAsync(id);

        Assert.Equal(expected + 1, updated!.Version);
    }

    [Fact]
    public async Task Delete_should_throw_when_version_conflicts()
    {
        var store = new InMemoryUserIdentifierStore(new TenantContext(TenantKeys.Single));
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        var identifier = UserIdentifier.Create(id, TenantKey.Single, TestUsers.Admin, UserIdentifierType.Email, "a@test.com", "a@test.com", now);
        await store.AddAsync(identifier);

        var copy1 = await store.GetByIdAsync(id);
        var copy2 = await store.GetByIdAsync(id);

        var expected1 = copy1!.Version;
        copy1.MarkDeleted(now);
        await store.SaveAsync(copy1, expected1);

        await Assert.ThrowsAsync<UAuthConcurrencyException>(async () =>
        {
            await store.DeleteAsync(copy2!.Id, copy2!.Version, DeleteMode.Soft, now);
        });
    }

    [Fact]
    public async Task Parallel_SetPrimary_should_conflict_deterministic()
    {
        var store = new InMemoryUserIdentifierStore(new TenantContext(TenantKeys.Single));
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        var identifier = UserIdentifier.Create(id, TenantKey.Single, TestUsers.Admin, UserIdentifierType.Email, "a@test.com", "a@test.com", now);
        await store.AddAsync(identifier);

        int success = 0;
        int conflicts = 0;

        var barrier = new Barrier(2);

        var tasks = Enumerable.Range(0, 2)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    var copy = await store.GetByIdAsync(id);

                    barrier.SignalAndWait();

                    var expected = copy!.Version;
                    copy.SetPrimary(now);
                    await store.SaveAsync(copy, expected);
                    Interlocked.Increment(ref success);
                }
                catch (UAuthConcurrencyException)
                {
                    Interlocked.Increment(ref conflicts);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, success);
        Assert.Equal(1, conflicts);

        var final = await store.GetByIdAsync(id);
        Assert.True(final!.IsPrimary);
        Assert.Equal(1, final.Version);
    }

    [Fact]
    public async Task Update_should_throw_concurrency_when_versions_conflict()
    {
        var store = new InMemoryUserIdentifierStore(new TenantContext(TenantKeys.Single));
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var tenant = TenantKey.Single;

        var identifier = UserIdentifier.Create(id, TenantKey.Single, TestUsers.Admin, UserIdentifierType.Email, "a@test.com", "a@test.com", now);
        await store.AddAsync(identifier);

        var copy1 = await store.GetByIdAsync(id);
        var copy2 = await store.GetByIdAsync(id);

        var expected1 = copy1!.Version;
        copy1.ChangeValue("b@test.com", "b@test.com", now);
        await store.SaveAsync(copy1, expected1);

        var expected2 = copy2!.Version;
        copy2.ChangeValue("c@test.com", "c@test.com", now);

        await Assert.ThrowsAsync<UAuthConcurrencyException>(async () =>
        {
            await store.SaveAsync(copy2, expected2);
        });
    }

    [Fact]
    public async Task Parallel_updates_should_result_in_single_success_deterministic()
    {
        var store = new InMemoryUserIdentifierStore(new TenantContext(TenantKeys.Single));
        var now = DateTimeOffset.UtcNow;
        var tenant = TenantKey.Single;
        var id = Guid.NewGuid();

        var identifier = UserIdentifier.Create(id, TenantKey.Single, TestUsers.Admin, UserIdentifierType.Email, "a@test.com", "a@test.com", now);
        await store.AddAsync(identifier);

        int success = 0;
        int conflicts = 0;

        var barrier = new Barrier(2);

        var tasks = Enumerable.Range(0, 2)
            .Select(i => Task.Run(async () =>
            {
                try
                {
                    var copy = await store.GetByIdAsync(id);
                    barrier.SignalAndWait();
                    var expected = copy!.Version;

                    var newValue = i == 0
                        ? "x@test.com"
                        : "y@test.com";

                    copy.ChangeValue(newValue, newValue, now);
                    await store.SaveAsync(copy, expected);
                    Interlocked.Increment(ref success);
                }
                catch (UAuthConcurrencyException)
                {
                    Interlocked.Increment(ref conflicts);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, success);
        Assert.Equal(1, conflicts);

        var final = await store.GetByIdAsync(id);

        Assert.NotNull(final);
        Assert.Equal(1, final!.Version);
    }

    [Fact]
    public async Task High_contention_updates_should_allow_only_one_success()
    {
        var store = new InMemoryUserIdentifierStore(new TenantContext(TenantKeys.Single));
        var now = DateTimeOffset.UtcNow;
        var tenant = TenantKey.Single;
        var id = Guid.NewGuid();

        var identifier = UserIdentifier.Create(id, TenantKey.Single, TestUsers.Admin, UserIdentifierType.Email, "initial@test.com", "initial@test.com", now);
        await store.AddAsync(identifier);

        int success = 0;
        int conflicts = 0;

        var tasks = Enumerable.Range(0, 20)
            .Select(i => Task.Run(async () =>
            {
                try
                {
                    var copy = await store.GetByIdAsync(id);
                    var expected = copy!.Version;

                    var newValue = $"user{i}@test.com";

                    copy.ChangeValue(newValue, newValue, now);

                    await store.SaveAsync(copy, expected);

                    Interlocked.Increment(ref success);
                }
                catch (UAuthConcurrencyException)
                {
                    Interlocked.Increment(ref conflicts);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.True(success >= 1);
        Assert.Equal(20, success + conflicts);

        var final = await store.GetByIdAsync(id);
        Assert.Equal(success, final!.Version);
    }

    [Fact]
    public async Task High_contention_SetPrimary_should_allow_only_one_deterministic()
    {
        var store = new InMemoryUserIdentifierStore(new TenantContext(TenantKeys.Single));
        var now = DateTimeOffset.UtcNow;
        var tenant = TenantKey.Single;

        var id = Guid.NewGuid();

        var identifier = UserIdentifier.Create(id, TenantKey.Single, TestUsers.Admin, UserIdentifierType.Email, "primary@test.com", "primary@test.com", now);
        await store.AddAsync(identifier);

        int success = 0;
        int conflicts = 0;

        var barrier = new Barrier(20);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    var copy = await store.GetByIdAsync(id);

                    barrier.SignalAndWait();

                    var expected = copy!.Version;
                    copy.SetPrimary(now);
                    await store.SaveAsync(copy, expected);
                    Interlocked.Increment(ref success);
                }
                catch (UAuthConcurrencyException)
                {
                    Interlocked.Increment(ref conflicts);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, success);
        Assert.Equal(19, conflicts);

        var final = await store.GetByIdAsync(id);

        Assert.True(final!.IsPrimary);
        Assert.Equal(1, final.Version);
    }

    [Fact]
    public async Task Two_identifiers_racing_for_primary_should_allow()
    {
        var store = new InMemoryUserIdentifierStore(new TenantContext(TenantKeys.Single));
        var now = DateTimeOffset.UtcNow;
        var tenant = TenantKey.Single;
        var user = TestUsers.Admin;

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var identifier1 = UserIdentifier.Create(id1, TenantKey.Single, TestUsers.Admin, UserIdentifierType.Email, "a@test.com", "a@test.com", now);
        var identifier2 = UserIdentifier.Create(id2, TenantKey.Single, TestUsers.Admin, UserIdentifierType.Email, "b@test.com", "b@test.com", now);

        await store.AddAsync(identifier1);
        await store.AddAsync(identifier2);

        int success = 0;
        int conflicts = 0;

        var barrier = new Barrier(2);

        var tasks = new[]
        {
        Task.Run(async () =>
        {
            try
            {
                var copy = await store.GetByIdAsync(id1);

                barrier.SignalAndWait();

                var expected = copy!.Version;
                copy.SetPrimary(now);

                await store.SaveAsync(copy, expected);

                Interlocked.Increment(ref success);
            }
            catch (UAuthConcurrencyException)
            {
                Interlocked.Increment(ref conflicts);
            }
        }),
        Task.Run(async () =>
        {
            try
            {
                var copy = await store.GetByIdAsync(id2);

                barrier.SignalAndWait();

                var expected = copy!.Version;
                copy.SetPrimary(now);

                await store.SaveAsync(copy, expected);

                Interlocked.Increment(ref success);
            }
            catch (UAuthConcurrencyException)
            {
                Interlocked.Increment(ref conflicts);
            }
        })
    };

        await Task.WhenAll(tasks);

        Assert.Equal(2, success);
        Assert.Equal(0, conflicts);

        var all = await store.GetByUserAsync(user);

        var primaries = all
            .Where(x => x.Type == UserIdentifierType.Email && x.IsPrimary)
            .ToList();

        Assert.Single(primaries);
    }
}
