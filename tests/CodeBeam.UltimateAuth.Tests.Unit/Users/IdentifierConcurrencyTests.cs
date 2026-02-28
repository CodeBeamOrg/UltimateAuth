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
        var store = new InMemoryUserIdentifierStore();
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        var identifier = new UserIdentifier
        {
            Id = id,
            Tenant = TenantKey.Single,
            UserKey = TestUsers.Admin,
            Type = UserIdentifierType.Email,
            Value = "a@test.com",
            NormalizedValue = "a@test.com",
            CreatedAt = now
        };

        await store.CreateAsync(TenantKey.Single, identifier);

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
        var store = new InMemoryUserIdentifierStore();
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        var identifier = new UserIdentifier
        {
            Id = id,
            Tenant = TenantKey.Single,
            UserKey = TestUsers.Admin,
            Type = UserIdentifierType.Email,
            Value = "a@test.com",
            NormalizedValue = "a@test.com",
            CreatedAt = now
        };

        await store.CreateAsync(TenantKey.Single, identifier);

        var copy1 = await store.GetByIdAsync(id);
        var copy2 = await store.GetByIdAsync(id);

        var expected1 = copy1!.Version;
        copy1.SoftDelete(now);
        await store.SaveAsync(copy1, expected1);

        await Assert.ThrowsAsync<UAuthConcurrencyException>(async () =>
        {
            await store.DeleteAsync(copy2!, copy2!.Version, DeleteMode.Soft, now);
        });
    }

    [Fact]
    public async Task Parallel_SetPrimary_should_conflict_deterministic()
    {
        var store = new InMemoryUserIdentifierStore();
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        var identifier = new UserIdentifier
        {
            Id = id,
            Tenant = TenantKey.Single,
            UserKey = TestUsers.Admin,
            Type = UserIdentifierType.Email,
            Value = "a@test.com",
            NormalizedValue = "a@test.com",
            CreatedAt = now
        };

        await store.CreateAsync(TenantKey.Single, identifier);

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
        var store = new InMemoryUserIdentifierStore();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var tenant = TenantKey.Single;

        var identifier = new UserIdentifier
        {
            Id = id,
            Tenant = tenant,
            UserKey = TestUsers.Admin,
            Type = UserIdentifierType.Email,
            Value = "a@test.com",
            NormalizedValue = "a@test.com",
            CreatedAt = now
        };

        await store.CreateAsync(tenant, identifier);

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
    public async Task Parallel_updates_should_result_in_single_success()
    {
        var store = new InMemoryUserIdentifierStore();
        var now = DateTimeOffset.UtcNow;
        var tenant = TenantKey.Single;
        var id = Guid.NewGuid();

        var identifier = new UserIdentifier
        {
            Id = id,
            Tenant = tenant,
            UserKey = TestUsers.Admin,
            Type = UserIdentifierType.Email,
            Value = "a@test.com",
            NormalizedValue = "a@test.com",
            CreatedAt = now
        };

        await store.CreateAsync(tenant, identifier);

        int success = 0;
        int conflicts = 0;

        var task1 = Task.Run(async () =>
        {
            try
            {
                var copy = await store.GetByIdAsync(id);
                var expected = copy!.Version;
                copy.ChangeValue("x@test.com", "x@test.com", now);
                await store.SaveAsync(copy, expected);
                Interlocked.Increment(ref success);
            }
            catch (UAuthConcurrencyException)
            {
                Interlocked.Increment(ref conflicts);
            }
        });

        var task2 = Task.Run(async () =>
        {
            try
            {
                var copy = await store.GetByIdAsync(id);
                var expected = copy!.Version;
                copy.ChangeValue("y@test.com", "y@test.com", now);
                await store.SaveAsync(copy, expected);
                Interlocked.Increment(ref success);
            }
            catch (UAuthConcurrencyException)
            {
                Interlocked.Increment(ref conflicts);
            }
        });

        await Task.WhenAll(task1, task2);

        Assert.Equal(1, success);
        Assert.Equal(1, conflicts);
    }

    [Fact]
    public async Task High_contention_updates_should_allow_only_one_success()
    {
        var store = new InMemoryUserIdentifierStore();
        var now = DateTimeOffset.UtcNow;
        var tenant = TenantKey.Single;
        var id = Guid.NewGuid();

        var identifier = new UserIdentifier
        {
            Id = id,
            Tenant = tenant,
            UserKey = TestUsers.Admin,
            Type = UserIdentifierType.Email,
            Value = "initial@test.com",
            NormalizedValue = "initial@test.com",
            CreatedAt = now
        };

        await store.CreateAsync(tenant, identifier);

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
        var store = new InMemoryUserIdentifierStore();
        var now = DateTimeOffset.UtcNow;
        var tenant = TenantKey.Single;

        var id = Guid.NewGuid();

        var identifier = new UserIdentifier
        {
            Id = id,
            Tenant = tenant,
            UserKey = TestUsers.Admin,
            Type = UserIdentifierType.Email,
            Value = "primary@test.com",
            NormalizedValue = "primary@test.com",
            CreatedAt = now
        };

        await store.CreateAsync(tenant, identifier);

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
}
