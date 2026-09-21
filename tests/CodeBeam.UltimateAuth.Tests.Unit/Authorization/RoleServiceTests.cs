using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Reference;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class RoleServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);

    // =========================================================
    // Create
    // =========================================================

    [Fact]
    public async Task CreateAsync_WhenValid_CreatesAndPersistsRole()
    {
        var f = new Fixture();
        var context = f.Context("roles.create");

        var permissions = new[]
        {
            Permission.From("users.read"),
            Permission.From("users.write")
        };

        Role? captured = null;

        f.RoleStore
            .Setup(x => x.AddAsync(
                It.IsAny<Role>(),
                It.IsAny<CancellationToken>()))
            .Callback<Role, CancellationToken>((role, _) => captured = role)
            .Returns(Task.CompletedTask);

        var result = await f.Sut.CreateAsync(
            context,
            "  Administrators  ",
            permissions);

        result.Should().BeSameAs(captured);

        captured.Should().NotBeNull();
        captured!.Tenant.Should().Be(context.ResourceTenant);
        captured.Name.Should().Be("Administrators");
        captured.NormalizedName.Should().Be("ADMINISTRATORS");
        captured.CreatedAt.Should().Be(Now);
        captured.Permissions.Should().BeEquivalentTo(permissions);

        f.RoleFactory.Verify(
            x => x.Create(context.ResourceTenant),
            Times.Once);
    }

    // =========================================================
    // Rename
    // =========================================================

    [Fact]
    public async Task RenameAsync_WhenRoleDoesNotExist_ThrowsNotFound()
    {
        var f = new Fixture();
        var context = f.Context("roles.rename");
        var roleId = RoleId.New();

        f.RoleStore
            .Setup(x => x.GetAsync(
                new RoleKey(context.ResourceTenant, roleId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var act = () => f.Sut.RenameAsync(
            context,
            roleId,
            "New Name");

        var exception = await act.Should()
            .ThrowAsync<UAuthNotFoundException>();

        exception.Which.Code.Should().Be("role_not_found");

        f.RoleStore.Verify(
            x => x.SaveAsync(
                It.IsAny<Role>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RenameAsync_WhenRoleIsDeleted_ThrowsNotFound()
    {
        var f = new Fixture();
        var context = f.Context("roles.rename");
        var role = f.Role("Old Name", version: 4);

        role.MarkDeleted(Now.AddMinutes(-1));

        f.SetupGetRole(context, role);

        var act = () => f.Sut.RenameAsync(
            context,
            role.Id,
            "New Name");

        var exception = await act.Should()
            .ThrowAsync<UAuthNotFoundException>();

        exception.Which.Code.Should().Be("role_not_found");

        f.RoleStore.Verify(
            x => x.SaveAsync(
                It.IsAny<Role>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RenameAsync_WhenValid_RenamesAndSavesWithOriginalVersion()
    {
        var f = new Fixture();
        var context = f.Context("roles.rename");
        var role = f.Role("Old Name", version: 7);

        f.SetupGetRole(context, role);

        f.RoleStore
            .Setup(x => x.SaveAsync(
                role,
                7,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await f.Sut.RenameAsync(
            context,
            role.Id,
            "  New Name  ");

        role.Name.Should().Be("New Name");
        role.NormalizedName.Should().Be("NEW NAME");
        role.UpdatedAt.Should().Be(Now);

        f.RoleStore.Verify(
            x => x.SaveAsync(
                role,
                7,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // Delete
    // =========================================================

    [Fact]
    public async Task DeleteAsync_WhenRoleDoesNotExist_ThrowsNotFound()
    {
        var f = new Fixture();
        var context = f.Context("roles.delete");
        var roleId = RoleId.New();

        f.RoleStore
            .Setup(x => x.GetAsync(
                new RoleKey(context.ResourceTenant, roleId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var act = () => f.Sut.DeleteAsync(
            context,
            roleId,
            DeleteMode.Soft);

        var exception = await act.Should()
            .ThrowAsync<UAuthNotFoundException>();

        exception.Which.Code.Should().Be("role_not_found");

        f.UserRoleStore.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(DeleteMode.Soft)]
    [InlineData(DeleteMode.Hard)]
    public async Task DeleteAsync_WhenValid_RemovesAssignmentsAndUsesRequestedMode(
        DeleteMode mode)
    {
        var f = new Fixture();
        var context = f.Context("roles.delete");
        var role = f.Role("Admin", version: 9);

        f.SetupGetRole(context, role);

        f.UserRoleStore
            .Setup(x => x.CountAssignmentsAsync(
                role.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(13);

        f.UserRoleStore
            .Setup(x => x.RemoveAssignmentsByRoleAsync(
                role.Id,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        f.RoleStore
            .Setup(x => x.DeleteAsync(
                new RoleKey(context.ResourceTenant, role.Id),
                9,
                mode,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.DeleteAsync(
            context,
            role.Id,
            mode);

        result.RoleId.Should().Be(role.Id);
        result.RemovedAssignments.Should().Be(13);
        result.Mode.Should().Be(mode);
        result.DeletedAt.Should().Be(Now);

        f.UserRoleStore.Verify(
            x => x.RemoveAssignmentsByRoleAsync(
                role.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);

        f.RoleStore.Verify(
            x => x.DeleteAsync(
                new RoleKey(context.ResourceTenant, role.Id),
                9,
                mode,
                Now,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // Set Permissions
    // =========================================================

    [Fact]
    public async Task SetPermissionsAsync_WhenRoleDoesNotExist_ThrowsNotFound()
    {
        var f = new Fixture();
        var context = f.Context("roles.permissions.set");
        var roleId = RoleId.New();

        f.RoleStore
            .Setup(x => x.GetAsync(
                new RoleKey(context.ResourceTenant, roleId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var act = () => f.Sut.SetPermissionsAsync(
            context,
            roleId,
            [Permission.From("users.read")]);

        var exception = await act.Should()
            .ThrowAsync<UAuthNotFoundException>();

        exception.Which.Code.Should().Be("role_not_found");
    }

    [Fact]
    public async Task SetPermissionsAsync_WhenRoleIsDeleted_ThrowsNotFound()
    {
        var f = new Fixture();
        var context = f.Context("roles.permissions.set");
        var role = f.Role("Admin", version: 3);

        role.MarkDeleted(Now.AddMinutes(-1));

        f.SetupGetRole(context, role);

        var act = () => f.Sut.SetPermissionsAsync(
            context,
            role.Id,
            [Permission.From("users.read")]);

        var exception = await act.Should()
            .ThrowAsync<UAuthNotFoundException>();

        exception.Which.Code.Should().Be("role_not_found");
    }

    [Fact]
    public async Task SetPermissionsAsync_WhenValid_UpdatesAndSavesWithOriginalVersion()
    {
        var f = new Fixture();
        var context = f.Context("roles.permissions.set");

        var role = f.Role("Admin", version: 12);

        f.SetupGetRole(context, role);

        f.RoleStore
            .Setup(x => x.SaveAsync(
                role,
                12,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var permissions = new[]
        {
        Permission.From("users.read")
    };

        await f.Sut.SetPermissionsAsync(
            context,
            role.Id,
            permissions);

        role.Permissions.Should().ContainSingle().Which.Should().Be(Permission.From("users.read"));

        role.UpdatedAt.Should().Be(Now);

        f.RoleStore.Verify(
            x => x.SaveAsync(
                role,
                12,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // Query
    // =========================================================

    [Fact]
    public async Task QueryAsync_ForwardsQueryAndReturnsStoreResult()
    {
        var f = new Fixture();
        var context = f.Context("roles.list");

        var query = new RoleQuery
        {
            Search = "admin",
            IncludeDeleted = true,
            PageNumber = 2,
            PageSize = 25,
            SortBy = "name",
            Descending = true
        };

        var roles = new[]
        {
            f.Role("Admin"),
            f.Role("Super Admin")
        };

        var expected = new PagedResult<Role>(
            roles,
            totalCount: 42,
            pageNumber: 2,
            pageSize: 25,
            sortBy: "name",
            descending: true);

        f.RoleStore
            .Setup(x => x.QueryAsync(
                query,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await f.Sut.QueryAsync(context, query);

        result.Should().BeSameAs(expected);

        f.RoleFactory.Verify(
            x => x.Create(context.ResourceTenant),
            Times.Once);
    }

    // =========================================================
    // Cancellation
    // =========================================================

    [Fact]
    public async Task CreateAsync_WhenAlreadyCancelled_ThrowsBeforeAccessExecution()
    {
        var f = new Fixture();
        var context = f.Context("roles.create");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => f.Sut.CreateAsync(
            context,
            "Admin",
            null,
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();

        f.AccessOrchestrator.VerifyNoOtherCalls();
        f.RoleFactory.VerifyNoOtherCalls();
    }

    // =========================================================
    // Fixture
    // =========================================================

    private sealed class Fixture
    {
        public Mock<IAccessOrchestrator> AccessOrchestrator { get; }
            = new(MockBehavior.Strict);

        public Mock<IRoleStoreFactory> RoleFactory { get; }
            = new(MockBehavior.Strict);

        public Mock<IRoleStore> RoleStore { get; }
            = new(MockBehavior.Strict);

        public Mock<IUserRoleStoreFactory> UserRoleFactory { get; }
            = new(MockBehavior.Strict);

        public Mock<IUserRoleStore> UserRoleStore { get; }
            = new(MockBehavior.Strict);

        public Mock<IClock> Clock { get; }
            = new(MockBehavior.Strict);

        public RoleService Sut { get; }

        public Fixture()
        {
            Clock
                .SetupGet(x => x.UtcNow)
                .Returns(Now);

            // AccessOrchestrator is deliberately transparent here.
            // RoleService behavior is the subject under test.
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
                    It.IsAny<AccessCommand<Role>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<AccessContext, AccessCommand<Role>, CancellationToken>(
                    async (_, command, ct) =>
                        await command.ExecuteAsync(ct));

            AccessOrchestrator
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<AccessContext>(),
                    It.IsAny<AccessCommand<DeleteRoleResult>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<AccessContext, AccessCommand<DeleteRoleResult>, CancellationToken>(
                    async (_, command, ct) =>
                        await command.ExecuteAsync(ct));

            AccessOrchestrator
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<AccessContext>(),
                    It.IsAny<AccessCommand<PagedResult<Role>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<AccessContext, AccessCommand<PagedResult<Role>>, CancellationToken>(
                    async (_, command, ct) =>
                        await command.ExecuteAsync(ct));

            RoleFactory
                .Setup(x => x.Create(It.IsAny<TenantKey>()))
                .Returns(RoleStore.Object);

            UserRoleFactory
                .Setup(x => x.Create(It.IsAny<TenantKey>()))
                .Returns(UserRoleStore.Object);

            Sut = new RoleService(
                AccessOrchestrator.Object,
                RoleFactory.Object,
                UserRoleFactory.Object,
                Clock.Object);
        }

        public AccessContext Context(string action)
            => TestAccessContext.WithAction(action);

        public Role Role(
            string name,
            long version = 0,
            IEnumerable<Permission>? permissions = null)
        {
            var role = Authorization.Role.Create(
                RoleId.New(),
                TenantKey.Single,
                name,
                permissions,
                Now.AddHours(-1));

            role.Version = version;
            return role;
        }

        public void SetupGetRole(
            AccessContext context,
            Role role)
        {
            RoleStore
                .Setup(x => x.GetAsync(
                    new RoleKey(context.ResourceTenant, role.Id),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(role);
        }
    }
}