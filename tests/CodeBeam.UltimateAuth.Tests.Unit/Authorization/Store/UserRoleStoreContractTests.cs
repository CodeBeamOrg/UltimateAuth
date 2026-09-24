using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Authorization.Contracts;

public abstract class UserRoleStoreContractTests
{
    protected abstract Task<IUserRoleStoreTestDatabase> CreateDatabaseAsync();

    protected virtual TenantKey Tenant => TenantKeys.Single;

    protected static readonly DateTimeOffset Now =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------
    // Assign
    // ---------------------------------------------------------

    [Fact]
    public async Task AssignAsync_WhenValid_PersistsAssignment()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();
        var roleId = RoleId.New();
        var assignedAt = Now.AddMinutes(-10);

        await store.AssignAsync(user, roleId, assignedAt);

        var result = await store.GetAssignmentsAsync(user);

        result.Should().ContainSingle();

        var assignment = result.Single();

        assignment.Tenant.Should().Be(Tenant);
        assignment.UserKey.Should().Be(user);
        assignment.RoleId.Should().Be(roleId);
        assignment.AssignedAt.Should().Be(assignedAt);
    }

    [Fact]
    public async Task AssignAsync_WhenSameRoleAlreadyAssigned_ThrowsConflict()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();
        var roleId = RoleId.New();

        await store.AssignAsync(user, roleId, Now);

        var act = () => store.AssignAsync(
            user,
            roleId,
            Now.AddMinutes(1));

        await act.Should()
            .ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task AssignAsync_SameUserCanHaveMultipleRoles()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var roleA = RoleId.New();
        var roleB = RoleId.New();

        await store.AssignAsync(user, roleA, Now);
        await store.AssignAsync(user, roleB, Now.AddMinutes(1));

        var result = await store.GetAssignmentsAsync(user);

        result.Should().HaveCount(2);

        result.Select(x => x.RoleId)
            .Should()
            .BeEquivalentTo([roleA, roleB]);
    }

    [Fact]
    public async Task AssignAsync_SameRoleCanBeAssignedToMultipleUsers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var userA = UserKey.New();
        var userB = UserKey.New();
        var roleId = RoleId.New();

        await store.AssignAsync(userA, roleId, Now);
        await store.AssignAsync(userB, roleId, Now.AddMinutes(1));

        var assignmentsA = await store.GetAssignmentsAsync(userA);
        var assignmentsB = await store.GetAssignmentsAsync(userB);

        assignmentsA.Should().ContainSingle();
        assignmentsB.Should().ContainSingle();

        assignmentsA.Single().RoleId.Should().Be(roleId);
        assignmentsB.Single().RoleId.Should().Be(roleId);
    }

    // ---------------------------------------------------------
    // GetAssignments
    // ---------------------------------------------------------

    [Fact]
    public async Task GetAssignmentsAsync_ReturnsOnlyRequestedUserAssignments()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var userA = UserKey.New();
        var userB = UserKey.New();

        var roleA = RoleId.New();
        var roleB = RoleId.New();

        await store.AssignAsync(userA, roleA, Now);
        await store.AssignAsync(userB, roleB, Now);

        var result = await store.GetAssignmentsAsync(userA);

        result.Should().ContainSingle();
        result.Single().UserKey.Should().Be(userA);
        result.Single().RoleId.Should().Be(roleA);
    }

    [Fact]
    public async Task GetAssignmentsAsync_WhenUserHasNoAssignments_ReturnsEmpty()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var result = await store.GetAssignmentsAsync(UserKey.New());

        result.Should().BeEmpty();
    }

    // ---------------------------------------------------------
    // Remove
    // ---------------------------------------------------------

    [Fact]
    public async Task RemoveAsync_WhenAssignmentExists_RemovesOnlyRequestedAssignment()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();

        var roleA = RoleId.New();
        var roleB = RoleId.New();

        await store.AssignAsync(user, roleA, Now);
        await store.AssignAsync(user, roleB, Now);

        await store.RemoveAsync(user, roleA);

        var result = await store.GetAssignmentsAsync(user);

        result.Should().ContainSingle();
        result.Single().RoleId.Should().Be(roleB);
    }

    [Fact]
    public async Task RemoveAsync_WhenAssignmentDoesNotExist_IsIdempotent()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var user = UserKey.New();
        var roleId = RoleId.New();

        var act = () => store.RemoveAsync(user, roleId);

        await act.Should().NotThrowAsync();

        var result = await store.GetAssignmentsAsync(user);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveAsync_DoesNotRemoveSameRoleFromOtherUser()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var userA = UserKey.New();
        var userB = UserKey.New();
        var roleId = RoleId.New();

        await store.AssignAsync(userA, roleId, Now);
        await store.AssignAsync(userB, roleId, Now);

        await store.RemoveAsync(userA, roleId);

        var assignmentsA = await store.GetAssignmentsAsync(userA);
        var assignmentsB = await store.GetAssignmentsAsync(userB);

        assignmentsA.Should().BeEmpty();

        assignmentsB.Should().ContainSingle();
        assignmentsB.Single().RoleId.Should().Be(roleId);
    }

    // ---------------------------------------------------------
    // Remove by role
    // ---------------------------------------------------------

    [Fact]
    public async Task RemoveAssignmentsByRoleAsync_RemovesRoleFromAllUsers()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var userA = UserKey.New();
        var userB = UserKey.New();

        var targetRole = RoleId.New();
        var otherRole = RoleId.New();

        await store.AssignAsync(userA, targetRole, Now);
        await store.AssignAsync(userA, otherRole, Now);
        await store.AssignAsync(userB, targetRole, Now);

        await store.RemoveAssignmentsByRoleAsync(targetRole);

        var assignmentsA = await store.GetAssignmentsAsync(userA);
        var assignmentsB = await store.GetAssignmentsAsync(userB);

        assignmentsA.Should().ContainSingle();
        assignmentsA.Single().RoleId.Should().Be(otherRole);

        assignmentsB.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveAssignmentsByRoleAsync_WhenNoAssignmentsExist_IsIdempotent()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var act = () =>
            store.RemoveAssignmentsByRoleAsync(RoleId.New());

        await act.Should().NotThrowAsync();
    }

    // ---------------------------------------------------------
    // Count
    // ---------------------------------------------------------

    [Fact]
    public async Task CountAssignmentsAsync_ReturnsNumberOfAssignmentsForRole()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var roleA = RoleId.New();
        var roleB = RoleId.New();

        await store.AssignAsync(UserKey.New(), roleA, Now);
        await store.AssignAsync(UserKey.New(), roleA, Now);
        await store.AssignAsync(UserKey.New(), roleA, Now);
        await store.AssignAsync(UserKey.New(), roleB, Now);

        var result = await store.CountAssignmentsAsync(roleA);

        result.Should().Be(3);
    }

    [Fact]
    public async Task CountAssignmentsAsync_WhenNoAssignmentsExist_ReturnsZero()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        var result =
            await store.CountAssignmentsAsync(RoleId.New());

        result.Should().Be(0);
    }

    // ---------------------------------------------------------
    // Tenant isolation
    // ---------------------------------------------------------

    [Fact]
    public async Task GetAssignmentsAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA = TestIds.Tenant("tenant-a");
        var tenantB = TestIds.Tenant("tenant-b");

        var storeA = db.CreateStore(tenantA);
        var storeB = db.CreateStore(tenantB);

        var user = UserKey.New();
        var roleId = RoleId.New();

        await storeA.AssignAsync(user, roleId, Now);

        var fromA = await storeA.GetAssignmentsAsync(user);
        var fromB = await storeB.GetAssignmentsAsync(user);

        fromA.Should().ContainSingle();
        fromB.Should().BeEmpty();
    }

    [Fact]
    public async Task CountAssignmentsAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA = TestIds.Tenant("tenant-a");
        var tenantB = TestIds.Tenant("tenant-b");

        var storeA = db.CreateStore(tenantA);
        var storeB = db.CreateStore(tenantB);

        var roleId = RoleId.New();

        await storeA.AssignAsync(UserKey.New(), roleId, Now);
        await storeA.AssignAsync(UserKey.New(), roleId, Now);

        await storeB.AssignAsync(UserKey.New(), roleId, Now);

        var countA = await storeA.CountAssignmentsAsync(roleId);
        var countB = await storeB.CountAssignmentsAsync(roleId);

        countA.Should().Be(2);
        countB.Should().Be(1);
    }

    [Fact]
    public async Task RemoveAssignmentsByRoleAsync_IsTenantIsolated()
    {
        await using var db = await CreateDatabaseAsync();

        var tenantA = TestIds.Tenant("tenant-a");
        var tenantB = TestIds.Tenant("tenant-b");

        var storeA = db.CreateStore(tenantA);
        var storeB = db.CreateStore(tenantB);

        var roleId = RoleId.New();

        var userA = UserKey.New();
        var userB = UserKey.New();

        await storeA.AssignAsync(userA, roleId, Now);
        await storeB.AssignAsync(userB, roleId, Now);

        await storeA.RemoveAssignmentsByRoleAsync(roleId);

        var assignmentsA =
            await storeA.GetAssignmentsAsync(userA);

        var assignmentsB =
            await storeB.GetAssignmentsAsync(userB);

        assignmentsA.Should().BeEmpty();

        assignmentsB.Should().ContainSingle();
        assignmentsB.Single().RoleId.Should().Be(roleId);
    }

    // ---------------------------------------------------------
    // Cancellation
    // ---------------------------------------------------------

    [Fact]
    public async Task Operations_WhenCancellationRequested_ThrowOperationCanceledException()
    {
        await using var db = await CreateDatabaseAsync();
        var store = db.CreateStore(Tenant);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var user = UserKey.New();
        var roleId = RoleId.New();

        var get = () =>
            store.GetAssignmentsAsync(user, cts.Token);

        var assign = () =>
            store.AssignAsync(user, roleId, Now, cts.Token);

        var remove = () =>
            store.RemoveAsync(user, roleId, cts.Token);

        var removeByRole = () =>
            store.RemoveAssignmentsByRoleAsync(roleId, cts.Token);

        var count = () =>
            store.CountAssignmentsAsync(roleId, cts.Token);

        await get.Should().ThrowAsync<OperationCanceledException>();
        await assign.Should().ThrowAsync<OperationCanceledException>();
        await remove.Should().ThrowAsync<OperationCanceledException>();
        await removeByRole.Should().ThrowAsync<OperationCanceledException>();
        await count.Should().ThrowAsync<OperationCanceledException>();
    }
}
