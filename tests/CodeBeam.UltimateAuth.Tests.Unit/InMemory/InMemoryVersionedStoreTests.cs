using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.InMemory;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.InMemory;

public sealed class InMemoryVersionedStoreTests
{
    [Fact]
    public async Task AddAsync_WithoutAtomicContext_ShouldPersistEntity()
    {
        var accessor = new InMemoryAtomicContextAccessor();
        var store = new TestStore(accessor);

        var entity = TestEntity.Create("entity-1", "initial");

        await store.AddAsync(entity);

        var stored = await store.GetAsync("entity-1");

        stored.Should().NotBeNull();
        stored!.Value.Should().Be("initial");
        stored.Version.Should().Be(0);
    }

    [Fact]
    public async Task AddAsync_WithAtomicContext_WhenRolledBack_ShouldRemoveEntity()
    {
        var accessor = new InMemoryAtomicContextAccessor();
        var atomic = new InMemoryAtomicContext();

        accessor.Current = atomic;

        var store = new TestStore(accessor);

        await store.AddAsync(
            TestEntity.Create("entity-1", "initial"));

        (await store.ExistsAsync("entity-1"))
            .Should()
            .BeTrue();

        atomic.Rollback();

        (await store.ExistsAsync("entity-1"))
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task SaveAsync_WithAtomicContext_WhenRolledBack_ShouldRestorePreviousEntity()
    {
        var accessor = new InMemoryAtomicContextAccessor();
        var store = new TestStore(accessor);

        await store.AddAsync(
            TestEntity.Create("entity-1", "initial"));

        var atomic = new InMemoryAtomicContext();
        accessor.Current = atomic;

        var entity = await store.GetAsync("entity-1");

        entity!.Value = "updated";

        await store.SaveAsync(
            entity,
            expectedVersion: 0);

        var updated = await store.GetAsync("entity-1");

        updated!.Value.Should().Be("updated");
        updated.Version.Should().Be(1);

        atomic.Rollback();

        var restored = await store.GetAsync("entity-1");

        restored.Should().NotBeNull();
        restored!.Value.Should().Be("initial");
        restored.Version.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_Hard_WithAtomicContext_WhenRolledBack_ShouldRestoreEntity()
    {
        var accessor = new InMemoryAtomicContextAccessor();
        var store = new TestStore(accessor);

        await store.AddAsync(
            TestEntity.Create("entity-1", "initial"));

        var atomic = new InMemoryAtomicContext();
        accessor.Current = atomic;

        await store.DeleteAsync(
            "entity-1",
            expectedVersion: 0,
            DeleteMode.Hard,
            DateTimeOffset.UtcNow);

        (await store.ExistsAsync("entity-1"))
            .Should()
            .BeFalse();

        atomic.Rollback();

        var restored = await store.GetAsync("entity-1");

        restored.Should().NotBeNull();
        restored!.Value.Should().Be("initial");
        restored.Version.Should().Be(0);
        restored.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_Soft_WithAtomicContext_WhenRolledBack_ShouldRestoreEntity()
    {
        var accessor = new InMemoryAtomicContextAccessor();
        var store = new TestStore(accessor);

        await store.AddAsync(
            TestEntity.Create("entity-1", "initial"));

        var atomic = new InMemoryAtomicContext();
        accessor.Current = atomic;

        var deletedAt =
            new DateTimeOffset(
                2026, 1, 1,
                12, 0, 0,
                TimeSpan.Zero);

        await store.DeleteAsync(
            "entity-1",
            expectedVersion: 0,
            DeleteMode.Soft,
            deletedAt);

        var deleted = await store.GetAsync("entity-1");

        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().Be(deletedAt);
        deleted.Version.Should().Be(1);

        atomic.Rollback();

        var restored = await store.GetAsync("entity-1");

        restored.Should().NotBeNull();
        restored!.IsDeleted.Should().BeFalse();
        restored.DeletedAt.Should().BeNull();
        restored.Version.Should().Be(0);
    }

    [Fact]
    public async Task MultipleOperations_WhenRolledBack_ShouldRestoreOriginalState()
    {
        var accessor = new InMemoryAtomicContextAccessor();
        var store = new TestStore(accessor);

        await store.AddAsync(
            TestEntity.Create("existing", "original"));

        var atomic = new InMemoryAtomicContext();
        accessor.Current = atomic;

        // ADD
        await store.AddAsync(
            TestEntity.Create("new", "created"));

        // UPDATE
        var existing = await store.GetAsync("existing");

        existing!.Value = "updated";

        await store.SaveAsync(
            existing,
            expectedVersion: 0);

        // DELETE newly-created entity
        await store.DeleteAsync(
            "new",
            expectedVersion: 0,
            DeleteMode.Hard,
            DateTimeOffset.UtcNow);

        atomic.Rollback();

        var restoredExisting =
            await store.GetAsync("existing");

        restoredExisting.Should().NotBeNull();
        restoredExisting!.Value.Should().Be("original");
        restoredExisting.Version.Should().Be(0);

        (await store.ExistsAsync("new"))
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Rollback_ShouldExecuteActionsInReverseOrder()
    {
        var atomic = new InMemoryAtomicContext();

        var calls = new List<int>();

        atomic.RegisterRollback(() => calls.Add(1));
        atomic.RegisterRollback(() => calls.Add(2));
        atomic.RegisterRollback(() => calls.Add(3));

        atomic.Rollback();

        calls.Should().ContainInOrder(3, 2, 1);
    }

    [Fact]
    public void Rollback_WhenActionFails_ShouldContinueExecutingRemainingActions()
    {
        var atomic = new InMemoryAtomicContext();

        var calls = new List<int>();

        atomic.RegisterRollback(
            () => calls.Add(1));

        atomic.RegisterRollback(
            () => throw new InvalidOperationException("rollback failed"));

        atomic.RegisterRollback(
            () => calls.Add(3));

        var act = atomic.Rollback;

        act.Should()
            .Throw<AggregateException>()
            .Which.InnerExceptions.Should()
            .ContainSingle()
            .Which.Message.Should()
            .Be("rollback failed");

        calls.Should().ContainInOrder(3, 1);
    }

    [Fact]
    public void RegisterRollback_WithNullAction_ShouldThrow()
    {
        var atomic = new InMemoryAtomicContext();

        var act = () =>
            atomic.RegisterRollback(null!);

        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AtomicContextAccessor_ShouldFlowAcrossAwait()
    {
        var accessor = new InMemoryAtomicContextAccessor();
        var atomic = new InMemoryAtomicContext();

        accessor.Current = atomic;

        await Task.Yield();

        accessor.Current.Should().BeSameAs(atomic);
    }

    private sealed class TestStore
        : InMemoryVersionedStore<TestEntity, string>
    {
        public TestStore(
            InMemoryAtomicContextAccessor atomicContext)
            : base(atomicContext)
        {
        }

        protected override string GetKey(
            TestEntity entity)
            => entity.Id;
    }

    private sealed class TestEntity :
        IVersionedEntity,
        IEntitySnapshot<TestEntity>,
        ISoftDeletable<TestEntity>
    {
        public required string Id { get; init; }

        public required string Value { get; set; }

        public long Version { get; set; }

        public bool IsDeleted { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public static TestEntity Create(
            string id,
            string value)
        {
            return new TestEntity
            {
                Id = id,
                Value = value,
                Version = 0
            };
        }

        public TestEntity Snapshot()
        {
            return new TestEntity
            {
                Id = Id,
                Value = Value,
                Version = Version,
                IsDeleted = IsDeleted,
                DeletedAt = DeletedAt
            };
        }

        public TestEntity MarkDeleted(
            DateTimeOffset now)
        {
            var snapshot = Snapshot();

            snapshot.IsDeleted = true;
            snapshot.DeletedAt = now;

            return snapshot;
        }
    }
}
