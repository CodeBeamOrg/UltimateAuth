using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Reference;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class UserRoleServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);

    // =========================================================
    // Assign
    // =========================================================

    [Fact]
    public async Task AssignAsync_WhenRoleExists_NormalizesRoleNameAndAssignsTargetUser()
    {
        var f = new Fixture();
        var context = f.Context("roles.assign");
        var target = UserKey.New();

        var role = f.Role("Administrators");

        f.RoleStore
            .Setup(x => x.GetByNameAsync(
                "ADMINISTRATORS",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        f.UserRoleStore
            .Setup(x => x.AssignAsync(
                target,
                role.Id,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.AssignAsync(
            context,
            target,
            "  administrators  ");

        f.RoleStore.Verify(x => x.GetByNameAsync(
            "ADMINISTRATORS",
            It.IsAny<CancellationToken>()),
            Times.Once);

        f.UserRoleStore.Verify(x => x.AssignAsync(
            target,
            role.Id,
            Now,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AssignAsync_WhenRoleDoesNotExist_ThrowsNotFound()
    {
        var f = new Fixture();
        var context = f.Context("roles.assign");
        var target = UserKey.New();

        f.RoleStore
            .Setup(x => x.GetByNameAsync(
                "MISSING",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var act = () => f.Sut.AssignAsync(
            context,
            target,
            "missing");

        var exception = await act.Should()
            .ThrowAsync<UAuthNotFoundException>();

        exception.Which.Code.Should().Be("role_not_found");

        f.UserRoleStore.Verify(
            x => x.AssignAsync(
                It.IsAny<UserKey>(),
                It.IsAny<RoleId>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AssignAsync_WhenRoleIsDeleted_ThrowsNotFound()
    {
        var f = new Fixture();
        var context = f.Context("roles.assign");
        var target = UserKey.New();

        var role = f.Role("Admin");
        role.MarkDeleted(Now.AddMinutes(-1));

        f.RoleStore
            .Setup(x => x.GetByNameAsync(
                "ADMIN",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var act = () => f.Sut.AssignAsync(
            context,
            target,
            "admin");

        var exception = await act.Should()
            .ThrowAsync<UAuthNotFoundException>();

        exception.Which.Code.Should().Be("role_not_found");

        f.UserRoleStore.Verify(
            x => x.AssignAsync(
                It.IsAny<UserKey>(),
                It.IsAny<RoleId>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AssignAsync_UsesResourceTenantForBothStores()
    {
        var f = new Fixture();
        var context = f.Context("roles.assign");
        var target = UserKey.New();
        var role = f.Role("Admin");

        f.RoleStore
            .Setup(x => x.GetByNameAsync(
                "ADMIN",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        f.UserRoleStore
            .Setup(x => x.AssignAsync(
                target,
                role.Id,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.AssignAsync(
            context,
            target,
            "Admin");

        f.RoleFactory.Verify(
            x => x.Create(context.ResourceTenant),
            Times.Once);

        f.UserRoleFactory.Verify(
            x => x.Create(context.ResourceTenant),
            Times.Once);
    }

    // =========================================================
    // Remove
    // =========================================================

    [Fact]
    public async Task RemoveAsync_WhenRoleExists_NormalizesRoleNameAndRemovesAssignment()
    {
        var f = new Fixture();
        var context = f.Context("roles.remove");
        var target = UserKey.New();

        var role = f.Role("Operators");

        f.RoleStore
            .Setup(x => x.GetByNameAsync(
                "OPERATORS",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        f.UserRoleStore
            .Setup(x => x.RemoveAsync(
                target,
                role.Id,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.RemoveAsync(
            context,
            target,
            "  operators ");

        f.UserRoleStore.Verify(x => x.RemoveAsync(
            target,
            role.Id,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WhenRoleDoesNotExist_IsIdempotent()
    {
        var f = new Fixture();
        var context = f.Context("roles.remove");
        var target = UserKey.New();

        f.RoleStore
            .Setup(x => x.GetByNameAsync(
                "MISSING",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var act = () => f.Sut.RemoveAsync(
            context,
            target,
            "missing");

        await act.Should().NotThrowAsync();

        f.UserRoleStore.Verify(
            x => x.RemoveAsync(
                It.IsAny<UserKey>(),
                It.IsAny<RoleId>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WhenRoleIsDeleted_StillRemovesAssignment()
    {
        var f = new Fixture();
        var context = f.Context("roles.remove");
        var target = UserKey.New();

        var role = f.Role("Legacy");
        role.MarkDeleted(Now.AddMinutes(-5));

        f.RoleStore
            .Setup(x => x.GetByNameAsync(
                "LEGACY",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        f.UserRoleStore
            .Setup(x => x.RemoveAsync(
                target,
                role.Id,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.RemoveAsync(
            context,
            target,
            "legacy");

        f.UserRoleStore.Verify(x => x.RemoveAsync(
            target,
            role.Id,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // GetRoles
    // =========================================================

    [Fact]
    public async Task GetRolesAsync_WhenAssignmentsExist_JoinsAssignmentsWithRoles()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        var admin = f.Role("Admin");
        var auditor = f.Role("Auditor");

        var adminAssignedAt = Now.AddDays(-10);
        var auditorAssignedAt = Now.AddDays(-5);

        var assignments = new[]
        {
            f.Assignment(target, admin.Id, adminAssignedAt),
            f.Assignment(target, auditor.Id, auditorAssignedAt)
        };

        f.UserRoleStore
            .Setup(x => x.GetAssignmentsAsync(
                target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignments);

        f.RoleStore
            .Setup(x => x.GetByIdsAsync(
                It.Is<IReadOnlyCollection<RoleId>>(ids =>
                    ids.Count == 2 &&
                    ids.Contains(admin.Id) &&
                    ids.Contains(auditor.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { admin, auditor });

        var result = await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest());

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);

        result.Items.Should().ContainEquivalentOf(
            new UserRoleInfo
            {
                Tenant = context.ResourceTenant,
                UserKey = target,
                RoleId = admin.Id,
                Name = "Admin",
                AssignedAt = adminAssignedAt
            });

        result.Items.Should().ContainEquivalentOf(
            new UserRoleInfo
            {
                Tenant = context.ResourceTenant,
                UserKey = target,
                RoleId = auditor.Id,
                Name = "Auditor",
                AssignedAt = auditorAssignedAt
            });
    }

    [Fact]
    public async Task GetRolesAsync_WhenAssignmentReferencesMissingRole_IgnoresOrphanAssignment()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        var existingRole = f.Role("Admin");
        var missingRoleId = RoleId.New();

        var assignments = new[]
        {
            f.Assignment(
                target,
                existingRole.Id,
                Now.AddDays(-2)),

            f.Assignment(
                target,
                missingRoleId,
                Now.AddDays(-1))
        };

        f.UserRoleStore
            .Setup(x => x.GetAssignmentsAsync(
                target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignments);

        f.RoleStore
            .Setup(x => x.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<RoleId>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingRole });

        var result = await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest());

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();

        result.Items[0].RoleId.Should().Be(existingRole.Id);
        result.Items[0].Name.Should().Be("Admin");
    }

    [Fact]
    public async Task GetRolesAsync_AppliesPaginationAfterJoin()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        var role1 = f.Role("Role 1");
        var role2 = f.Role("Role 2");
        var role3 = f.Role("Role 3");

        var assignments = new[]
        {
            f.Assignment(target, role1.Id, Now.AddDays(-3)),
            f.Assignment(target, role2.Id, Now.AddDays(-2)),
            f.Assignment(target, role3.Id, Now.AddDays(-1))
        };

        f.UserRoleStore
            .Setup(x => x.GetAssignmentsAsync(
                target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignments);

        f.RoleStore
            .Setup(x => x.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<RoleId>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { role1, role2, role3 });

        var result = await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest
            {
                PageNumber = 2,
                PageSize = 1
            });

        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(1);

        result.Items.Should().ContainSingle();
        result.Items[0].RoleId.Should().Be(role2.Id);

        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task GetRolesAsync_NormalizesInvalidPagingValues()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        f.UserRoleStore
            .Setup(x => x.GetAssignmentsAsync(
                target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserRole>());

        f.RoleStore
            .Setup(x => x.GetByIdsAsync(
                It.Is<IReadOnlyCollection<RoleId>>(ids => ids.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Role>());

        var result = await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest
            {
                PageNumber = -10,
                PageSize = 0
            });

        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(250);
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRolesAsync_WhenPageSizeExceedsMaximum_ClampsPageSize()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        f.UserRoleStore
            .Setup(x => x.GetAssignmentsAsync(
                target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserRole>());

        f.RoleStore
            .Setup(x => x.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<RoleId>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Role>());

        var result = await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 5000,
                MaxPageSize = 100
            });

        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task GetRolesAsync_PreservesPagingMetadata()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        f.UserRoleStore
            .Setup(x => x.GetAssignmentsAsync(
                target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserRole>());

        f.RoleStore
            .Setup(x => x.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<RoleId>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Role>());

        var result = await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest
            {
                PageNumber = 3,
                PageSize = 20,
                SortBy = "name",
                Descending = true
            });

        result.PageNumber.Should().Be(3);
        result.PageSize.Should().Be(20);
        result.SortBy.Should().Be("name");
        result.Descending.Should().BeTrue();
    }

    [Fact]
    public async Task GetRolesAsync_UsesResourceTenantForBothStores()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        f.UserRoleStore
            .Setup(x => x.GetAssignmentsAsync(
                target,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserRole>());

        f.RoleStore
            .Setup(x => x.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<RoleId>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Role>());

        await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest());

        f.RoleFactory.Verify(
            x => x.Create(context.ResourceTenant),
            Times.Once);

        f.UserRoleFactory.Verify(
            x => x.Create(context.ResourceTenant),
            Times.Once);
    }

    // =========================================================
    // Cancellation
    // =========================================================

    [Fact]
    public async Task AssignAsync_WhenAlreadyCancelled_DoesNotExecuteAccessCommand()
    {
        var f = new Fixture();
        var context = f.Context("roles.assign");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => f.Sut.AssignAsync(
            context,
            UserKey.New(),
            "Admin",
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();

        f.AccessOrchestrator.VerifyNoOtherCalls();
        f.RoleFactory.VerifyNoOtherCalls();
        f.UserRoleFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetRolesAsync_WhenSortByNameAscending_SortsByName()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        var charlie = f.Role("Charlie");
        var alpha = f.Role("Alpha");
        var bravo = f.Role("Bravo");

        // Deliberately not alphabetic.
        var assignments = new[]
        {
        f.Assignment(target, charlie.Id, Now.AddDays(-3)),
        f.Assignment(target, alpha.Id, Now.AddDays(-2)),
        f.Assignment(target, bravo.Id, Now.AddDays(-1))
    };

        f.SetupGetRoles(target, assignments, charlie, alpha, bravo);

        var result = await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest
            {
                SortBy = nameof(UserRoleInfo.Name),
                Descending = false
            });

        result.Items
            .Select(x => x.Name)
            .Should()
            .ContainInOrder("Alpha", "Bravo", "Charlie");
    }

    [Fact]
    public async Task GetRolesAsync_WhenSortByNameDescending_SortsByNameDescending()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        var bravo = f.Role("Bravo");
        var charlie = f.Role("Charlie");
        var alpha = f.Role("Alpha");

        var assignments = new[]
        {
        f.Assignment(target, bravo.Id, Now.AddDays(-3)),
        f.Assignment(target, charlie.Id, Now.AddDays(-2)),
        f.Assignment(target, alpha.Id, Now.AddDays(-1))
    };

        f.SetupGetRoles(target, assignments, bravo, charlie, alpha);

        var result = await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest
            {
                SortBy = nameof(UserRoleInfo.Name),
                Descending = true
            });

        result.Items
            .Select(x => x.Name)
            .Should()
            .ContainInOrder("Charlie", "Bravo", "Alpha");
    }

    [Fact]
    public async Task GetRolesAsync_WhenSortByAssignedAtAscending_SortsByAssignedAt()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        var role1 = f.Role("Role 1");
        var role2 = f.Role("Role 2");
        var role3 = f.Role("Role 3");

        var oldest = Now.AddDays(-10);
        var middle = Now.AddDays(-5);
        var newest = Now.AddDays(-1);

        var assignments = new[]
        {
        f.Assignment(target, role1.Id, newest),
        f.Assignment(target, role2.Id, oldest),
        f.Assignment(target, role3.Id, middle)
    };

        f.SetupGetRoles(target, assignments, role1, role2, role3);

        var result = await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest
            {
                SortBy = nameof(UserRoleInfo.AssignedAt),
                Descending = false
            });

        result.Items
            .Select(x => x.AssignedAt)
            .Should()
            .ContainInOrder(oldest, middle, newest);
    }

    [Fact]
    public async Task GetRolesAsync_WhenSortByAssignedAtDescending_SortsByAssignedAtDescending()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        var role1 = f.Role("Role 1");
        var role2 = f.Role("Role 2");
        var role3 = f.Role("Role 3");

        var oldest = Now.AddDays(-10);
        var middle = Now.AddDays(-5);
        var newest = Now.AddDays(-1);

        var assignments = new[]
        {
        f.Assignment(target, role1.Id, middle),
        f.Assignment(target, role2.Id, oldest),
        f.Assignment(target, role3.Id, newest)
    };

        f.SetupGetRoles(target, assignments, role1, role2, role3);

        var result = await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest
            {
                SortBy = nameof(UserRoleInfo.AssignedAt),
                Descending = true
            });

        result.Items
            .Select(x => x.AssignedAt)
            .Should()
            .ContainInOrder(newest, middle, oldest);
    }

    [Fact]
    public async Task GetRolesAsync_WhenSortByIsNotSpecified_DefaultsToNameAscending()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        var zebra = f.Role("Zebra");
        var admin = f.Role("Admin");
        var manager = f.Role("Manager");

        var assignments = new[]
        {
        f.Assignment(target, zebra.Id, Now.AddDays(-3)),
        f.Assignment(target, admin.Id, Now.AddDays(-2)),
        f.Assignment(target, manager.Id, Now.AddDays(-1))
    };

        f.SetupGetRoles(target, assignments, zebra, admin, manager);

        var result = await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest());

        result.Items
            .Select(x => x.Name)
            .Should()
            .ContainInOrder("Admin", "Manager", "Zebra");
    }

    [Fact]
    public async Task GetRolesAsync_SortsBeforeApplyingPagination()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        var charlie = f.Role("Charlie");
        var alpha = f.Role("Alpha");
        var echo = f.Role("Echo");
        var bravo = f.Role("Bravo");
        var delta = f.Role("Delta");

        // Deliberately:
        // Charlie, Alpha, Echo, Bravo, Delta
        //
        // Correct sorted result:
        // Alpha, Bravo, Charlie, Delta, Echo
        var assignments = new[]
        {
        f.Assignment(target, charlie.Id, Now.AddMinutes(-5)),
        f.Assignment(target, alpha.Id, Now.AddMinutes(-4)),
        f.Assignment(target, echo.Id, Now.AddMinutes(-3)),
        f.Assignment(target, bravo.Id, Now.AddMinutes(-2)),
        f.Assignment(target, delta.Id, Now.AddMinutes(-1))
    };

        f.SetupGetRoles(
            target,
            assignments,
            charlie,
            alpha,
            echo,
            bravo,
            delta);

        var result = await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest
            {
                PageNumber = 2,
                PageSize = 2,
                SortBy = nameof(UserRoleInfo.Name)
            });

        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);

        result.Items
            .Select(x => x.Name)
            .Should()
            .ContainInOrder("Charlie", "Delta");
    }

    [Fact]
    public async Task GetRolesAsync_WhenNamesAreEqual_UsesRoleIdAsDeterministicTieBreaker()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");
        var target = UserKey.New();

        var lowerId = Role.FromProjection(
            RoleId.From(Guid.Parse("00000000-0000-0000-0000-000000000001")),
            TenantKey.Single,
            "Admin",
            [],
            Now.AddDays(-2),
            null,
            null,
            0);

        var higherId = Role.FromProjection(
            RoleId.From(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            TenantKey.Single,
            "Admin",
            [],
            Now.AddDays(-1),
            null,
            null,
            0);

        // Reverse storage order deliberately.
        var assignments = new[]
        {
        f.Assignment(target, higherId.Id, Now),
        f.Assignment(target, lowerId.Id, Now)
    };

        f.SetupGetRoles(
            target,
            assignments,
            higherId,
            lowerId);

        var result = await f.Sut.GetRolesAsync(
            context,
            target,
            new PageRequest
            {
                SortBy = nameof(UserRoleInfo.Name)
            });

        result.Items.Select(x => x.RoleId)
            .Should()
            .ContainInOrder(lowerId.Id, higherId.Id);
    }

    // =========================================================
    // Fixture
    // =========================================================

    private sealed class Fixture
    {
        public Mock<IAccessOrchestrator> AccessOrchestrator { get; }
            = new(MockBehavior.Strict);

        public Mock<IUserRoleStoreFactory> UserRoleFactory { get; }
            = new(MockBehavior.Strict);

        public Mock<IUserRoleStore> UserRoleStore { get; }
            = new(MockBehavior.Strict);

        public Mock<IRoleStoreFactory> RoleFactory { get; }
            = new(MockBehavior.Strict);

        public Mock<IRoleStore> RoleStore { get; }
            = new(MockBehavior.Strict);

        public Mock<IClock> Clock { get; }
            = new(MockBehavior.Strict);

        public UserRoleService Sut { get; }

        public Fixture()
        {
            Clock
                .SetupGet(x => x.UtcNow)
                .Returns(Now);

            AccessOrchestrator
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<AccessContext>(),
                    It.IsAny<AccessCommand>(),
                    It.IsAny<CancellationToken>()))
                .Returns<AccessContext, AccessCommand, CancellationToken>(
                    async (_, command, ct) =>
                        await command.ExecuteAsync(ct));

            AccessOrchestrator
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<AccessContext>(),
                    It.IsAny<AccessCommand<PagedResult<UserRoleInfo>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<
                    AccessContext,
                    AccessCommand<PagedResult<UserRoleInfo>>,
                    CancellationToken>(
                    async (_, command, ct) =>
                        await command.ExecuteAsync(ct));

            UserRoleFactory
                .Setup(x => x.Create(It.IsAny<TenantKey>()))
                .Returns(UserRoleStore.Object);

            RoleFactory
                .Setup(x => x.Create(It.IsAny<TenantKey>()))
                .Returns(RoleStore.Object);

            Sut = new UserRoleService(
                AccessOrchestrator.Object,
                UserRoleFactory.Object,
                RoleFactory.Object,
                Clock.Object);
        }

        public AccessContext Context(string action)
            => TestAccessContext.WithAction(action);

        public Role Role(string name)
            => global::CodeBeam.UltimateAuth.Authorization.Role.Create(
                RoleId.New(),
                TenantKey.Single,
                name,
                permissions: null,
                now: Now.AddHours(-1));

        public UserRole Assignment(
            UserKey user,
            RoleId roleId,
            DateTimeOffset assignedAt)
            => new()
            {
                Tenant = TenantKey.Single,
                UserKey = user,
                RoleId = roleId,
                AssignedAt = assignedAt
            };

        public void SetupGetRoles(
            UserKey target,
            IReadOnlyCollection<UserRole> assignments,
            params Role[] roles)
        {
            UserRoleStore
                .Setup(x => x.GetAssignmentsAsync(
                    target,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignments);

            RoleStore
                .Setup(x => x.GetByIdsAsync(
                    It.Is<IReadOnlyCollection<RoleId>>(ids =>
                        ids.Count == assignments.Count &&
                        assignments.All(a => ids.Contains(a.RoleId))),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(roles);
        }
    }
}
