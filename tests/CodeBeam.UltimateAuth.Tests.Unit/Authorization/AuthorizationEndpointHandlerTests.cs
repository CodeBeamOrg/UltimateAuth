using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Reference;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class AuthorizationEndpointHandlerTests
{
    // =========================================================
    // Check
    // =========================================================

    [Fact]
    public async Task CheckAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = new Fixture(isAuthenticated: false);
        var ctx = f.Http();

        var result = await f.Sut.CheckAsync(ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status401Unauthorized);

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Authorization.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckAsync_WhenResourceIsMissing_ReturnsBadRequest()
    {
        var f = new Fixture();

        var ctx = f.Json(new AuthorizationCheckRequest
        {
            Action = "orders.read",
            Resource = ""
        });

        var result = await f.Sut.CheckAsync(ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status400BadRequest);

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Authorization.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckAsync_WhenActionIsMissing_ReturnsBadRequest()
    {
        var f = new Fixture();

        var ctx = f.Json(new AuthorizationCheckRequest
        {
            Action = "",
            Resource = "orders"
        });

        var result = await f.Sut.CheckAsync(ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status400BadRequest);

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Authorization.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckAsync_WhenAllowed_CreatesExpectedAccessContextAndReturnsOk()
    {
        var f = new Fixture();
        var accessContext = f.AccessContext("orders.read");

        var ctx = f.Json(new AuthorizationCheckRequest
        {
            Action = "orders.read",
            Resource = "orders",
            ResourceId = "order-123"
        });

        f.SetupAccessContext(
            "orders.read",
            "orders",
            "order-123",
            accessContext);

        var authorizationResult = AuthorizationResult.Allow();

        f.Authorization
            .Setup(x => x.AuthorizeAsync(
                accessContext,
                ctx.RequestAborted))
            .ReturnsAsync(authorizationResult);

        var result = await f.Sut.CheckAsync(ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status200OK);

        f.Authorization.Verify(x => x.AuthorizeAsync(
            accessContext,
            ctx.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task CheckAsync_WhenDenied_ReturnsForbidden()
    {
        var f = new Fixture();
        var accessContext = f.AccessContext("orders.delete");

        var ctx = f.Json(new AuthorizationCheckRequest
        {
            Action = "orders.delete",
            Resource = "orders",
            ResourceId = "order-123"
        });

        f.SetupAccessContext(
            "orders.delete",
            "orders",
            "order-123",
            accessContext);

        f.Authorization
            .Setup(x => x.AuthorizeAsync(
                accessContext,
                ctx.RequestAborted))
            .ReturnsAsync(AuthorizationResult.Deny("access_denied"));

        var result = await f.Sut.CheckAsync(ctx);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();

        f.Authorization.Verify(x => x.AuthorizeAsync(
            accessContext,
            ctx.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task CheckAsync_WhenReauthenticationIsRequired_Returns428()
    {
        var f = new Fixture();
        var accessContext = f.AccessContext("orders.delete");

        var ctx = f.Json(new AuthorizationCheckRequest
        {
            Action = "orders.delete",
            Resource = "orders",
            ResourceId = "order-123"
        });

        f.SetupAccessContext(
            "orders.delete",
            "orders",
            "order-123",
            accessContext);

        f.Authorization
            .Setup(x => x.AuthorizeAsync(
                accessContext,
                ctx.RequestAborted))
            .ReturnsAsync(AuthorizationResult.ReauthRequired());

        var result = await f.Sut.CheckAsync(ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status428PreconditionRequired);
    }

    // =========================================================
    // Get My Roles
    // =========================================================

    [Fact]
    public async Task GetMyRolesAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = new Fixture(isAuthenticated: false);

        var result = await f.Sut.GetMyRolesAsync(f.Http());

        AssertStatusCode(
            result,
            StatusCodes.Status401Unauthorized);

        f.UserRoles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMyRolesAsync_WhenAuthenticated_UsesCurrentUser()
    {
        var f = new Fixture();
        var query = new RoleQuery
        {
            PageNumber = 2,
            PageSize = 20
        };

        var ctx = f.Json(query);
        var accessContext = f.AccessContext(
            UAuthActions.Authorization.Roles.GetSelf);

        f.SetupAccessContext(
            UAuthActions.Authorization.Roles.GetSelf,
            "authorization.roles",
            f.UserKey.Value,
            accessContext);

        var expected = EmptyRoles();

        f.UserRoles
            .Setup(x => x.GetRolesAsync(
                accessContext,
                f.UserKey,
                It.Is<RoleQuery>(q =>
                    q.PageNumber == 2 &&
                    q.PageSize == 20),
                ctx.RequestAborted))
            .ReturnsAsync(expected);

        var result = await f.Sut.GetMyRolesAsync(ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status200OK);

        f.UserRoles.Verify(x => x.GetRolesAsync(
            accessContext,
            f.UserKey,
            It.IsAny<RoleQuery>(),
            ctx.RequestAborted),
            Times.Once);
    }

    // =========================================================
    // Get User Roles
    // =========================================================

    [Fact]
    public async Task GetUserRolesAsync_WhenAuthenticated_UsesTargetUserAndAdminAction()
    {
        var f = new Fixture();
        var target = UserKey.New();

        var ctx = f.Json(new RoleQuery());

        var accessContext = f.AccessContext(
            UAuthActions.Authorization.Roles.GetAdmin);

        f.SetupAccessContext(
            UAuthActions.Authorization.Roles.GetAdmin,
            "authorization.roles",
            target.Value,
            accessContext);

        f.UserRoles
            .Setup(x => x.GetRolesAsync(
                accessContext,
                target,
                It.IsAny<RoleQuery>(),
                ctx.RequestAborted))
            .ReturnsAsync(EmptyRoles());

        var result = await f.Sut.GetUserRolesAsync(
            target,
            ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status200OK);

        f.UserRoles.Verify(x => x.GetRolesAsync(
            accessContext,
            target,
            It.IsAny<RoleQuery>(),
            ctx.RequestAborted),
            Times.Once);
    }

    // =========================================================
    // Assign
    // =========================================================

    [Fact]
    public async Task AssignRoleAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = new Fixture(isAuthenticated: false);
        var target = UserKey.New();

        var result = await f.Sut.AssignRoleAsync(
            target,
            f.Http());

        AssertStatusCode(
            result,
            StatusCodes.Status401Unauthorized);

        f.UserRoles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AssignRoleAsync_WhenAuthenticated_UsesTargetUserAndRoleName()
    {
        var f = new Fixture();
        var target = UserKey.New();

        var ctx = f.Json(new AssignRoleRequest
        {
            UserKey = target,
            RoleName = "Administrators"
        });

        var accessContext = f.AccessContext(
            UAuthActions.Authorization.Roles.AssignAdmin);

        f.SetupAccessContext(
            UAuthActions.Authorization.Roles.AssignAdmin,
            "authorization.roles",
            target.Value,
            accessContext);

        f.UserRoles
            .Setup(x => x.AssignAsync(
                accessContext,
                target,
                "Administrators",
                ctx.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.AssignRoleAsync(
            target,
            ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status200OK);

        f.UserRoles.Verify(x => x.AssignAsync(
            accessContext,
            target,
            "Administrators",
            ctx.RequestAborted),
            Times.Once);
    }

    // =========================================================
    // Remove
    // =========================================================

    [Fact]
    public async Task RemoveRoleAsync_WhenAuthenticated_UsesTargetUserAndRoleName()
    {
        var f = new Fixture();
        var target = UserKey.New();

        var ctx = f.Json(new RemoveRoleRequest
        {
            UserKey = target,
            RoleName = "Administrators"
        });

        var accessContext = f.AccessContext(
            UAuthActions.Authorization.Roles.RemoveAdmin);

        f.SetupAccessContext(
            UAuthActions.Authorization.Roles.RemoveAdmin,
            "authorization.roles",
            target.Value,
            accessContext);

        f.UserRoles
            .Setup(x => x.RemoveAsync(
                accessContext,
                target,
                "Administrators",
                ctx.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.RemoveRoleAsync(
            target,
            ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status200OK);

        f.UserRoles.Verify(x => x.RemoveAsync(
            accessContext,
            target,
            "Administrators",
            ctx.RequestAborted),
            Times.Once);
    }

    // =========================================================
    // Create Role
    // =========================================================

    [Fact]
    public async Task CreateRoleAsync_WhenAuthenticated_ForwardsRequest()
    {
        var f = new Fixture();

        var permissions = new[]
        {
            Permission.From("users.read")
        };

        var ctx = f.Json(new CreateRoleRequest
        {
            Name = "Administrators",
            Permissions = permissions
        });

        var accessContext = f.AccessContext(
            UAuthActions.Authorization.Roles.CreateAdmin);

        f.SetupAccessContext(
            UAuthActions.Authorization.Roles.CreateAdmin,
            "authorization.roles",
            null,
            accessContext);

        var role = Role.Create(
            RoleId.New(),
            TenantKey.Single,
            "Administrators",
            permissions,
            DateTimeOffset.UtcNow);

        f.Roles
            .Setup(x => x.CreateAsync(
                accessContext,
                "Administrators",
                It.Is<IEnumerable<Permission>>(p =>
                    p.SequenceEqual(permissions)),
                ctx.RequestAborted))
            .ReturnsAsync(role);

        var result = await f.Sut.CreateRoleAsync(ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status200OK);

        f.Roles.Verify(x => x.CreateAsync(
            accessContext,
            "Administrators",
            It.IsAny<IEnumerable<Permission>>(),
            ctx.RequestAborted),
            Times.Once);
    }

    // =========================================================
    // Rename Role
    // =========================================================

    [Fact]
    public async Task RenameRoleAsync_WhenAuthenticated_UsesRouteRoleId()
    {
        var f = new Fixture();
        var roleId = RoleId.New();

        var ctx = f.Json(new RenameRoleRequest
        {
            Id = roleId,
            Name = "Operators"
        });

        var accessContext = f.AccessContext(
            UAuthActions.Authorization.Roles.RenameAdmin);

        f.SetupAccessContext(
            UAuthActions.Authorization.Roles.RenameAdmin,
            "authorization.roles",
            roleId.ToString(),
            accessContext);

        f.Roles
            .Setup(x => x.RenameAsync(
                accessContext,
                roleId,
                "Operators",
                ctx.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.RenameRoleAsync(
            roleId,
            ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status200OK);
    }

    // =========================================================
    // Delete Role
    // =========================================================

    [Fact]
    public async Task DeleteRoleAsync_WhenAuthenticated_ForwardsDeleteMode()
    {
        var f = new Fixture();
        var roleId = RoleId.New();

        var ctx = f.Json(new DeleteRoleRequest
        {
            Id = roleId,
            Mode = DeleteMode.Hard
        });

        var accessContext = f.AccessContext(
            UAuthActions.Authorization.Roles.DeleteAdmin);

        f.SetupAccessContext(
            UAuthActions.Authorization.Roles.DeleteAdmin,
            "authorization.roles",
            roleId.ToString(),
            accessContext);

        var expected = new DeleteRoleResult
        {
            RoleId = roleId,
            Mode = DeleteMode.Hard,
            RemovedAssignments = 3,
            DeletedAt = DateTimeOffset.UtcNow
        };

        f.Roles
            .Setup(x => x.DeleteAsync(
                accessContext,
                roleId,
                DeleteMode.Hard,
                ctx.RequestAborted))
            .ReturnsAsync(expected);

        var result = await f.Sut.DeleteRoleAsync(
            roleId,
            ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status200OK);

        f.Roles.Verify(x => x.DeleteAsync(
            accessContext,
            roleId,
            DeleteMode.Hard,
            ctx.RequestAborted),
            Times.Once);
    }

    // =========================================================
    // Set Permissions
    // =========================================================

    [Fact]
    public async Task SetRolePermissionsAsync_WhenAuthenticated_ForwardsPermissions()
    {
        var f = new Fixture();
        var roleId = RoleId.New();

        var permissions = new[]
        {
            Permission.From("users.read")
        };

        var ctx = f.Json(new SetRolePermissionsRequest
        {
            RoleId = roleId,
            Permissions = permissions
        });

        var accessContext = f.AccessContext(
            UAuthActions.Authorization.Roles.SetPermissionsAdmin);

        f.SetupAccessContext(
            UAuthActions.Authorization.Roles.SetPermissionsAdmin,
            "authorization.roles",
            roleId.ToString(),
            accessContext);

        f.Roles
            .Setup(x => x.SetPermissionsAsync(
                accessContext,
                roleId,
                It.Is<IEnumerable<Permission>>(p =>
                    p.SequenceEqual(permissions)),
                ctx.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.SetRolePermissionsAsync(
            roleId,
            ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status200OK);
    }

    // =========================================================
    // Query Roles
    // =========================================================

    [Fact]
    public async Task QueryRolesAsync_WhenAuthenticated_ForwardsQuery()
    {
        var f = new Fixture();

        var ctx = f.Json(new RoleQuery
        {
            Search = "admin",
            IncludeDeleted = true,
            PageNumber = 2,
            PageSize = 10,
            SortBy = nameof(Role.Name),
            Descending = true
        });

        var accessContext = f.AccessContext(
            UAuthActions.Authorization.Roles.QueryAdmin);

        f.SetupAccessContext(
            UAuthActions.Authorization.Roles.QueryAdmin,
            "authorization.roles",
            null,
            accessContext);

        var expected = new PagedResult<Role>(
            [],
            0,
            2,
            10,
            nameof(Role.Name),
            true);

        f.Roles
            .Setup(x => x.QueryAsync(
                accessContext,
                It.Is<RoleQuery>(q =>
                    q.Search == "admin" &&
                    q.IncludeDeleted &&
                    q.PageNumber == 2 &&
                    q.PageSize == 10 &&
                    q.SortBy == nameof(Role.Name) &&
                    q.Descending),
                ctx.RequestAborted))
            .ReturnsAsync(expected);

        var result = await f.Sut.QueryRolesAsync(ctx);

        AssertStatusCode(
            result,
            StatusCodes.Status200OK);
    }

    [Fact]
    public async Task RenameRoleAsync_WhenBodyContainsDifferentId_UsesRouteRoleId()
    {
        var f = new Fixture();

        var routeRoleId = RoleId.New();
        var bodyRoleId = RoleId.New();

        var ctx = f.Json(new RenameRoleRequest
        {
            Id = bodyRoleId,
            Name = "Operators"
        });

        var accessContext = f.AccessContext(
            UAuthActions.Authorization.Roles.RenameAdmin);

        f.SetupAccessContext(
            UAuthActions.Authorization.Roles.RenameAdmin,
            "authorization.roles",
            routeRoleId.ToString(),
            accessContext);

        f.Roles
            .Setup(x => x.RenameAsync(
                accessContext,
                routeRoleId,
                "Operators",
                ctx.RequestAborted))
            .Returns(Task.CompletedTask);

        await f.Sut.RenameRoleAsync(routeRoleId, ctx);

        f.Roles.Verify(x => x.RenameAsync(
            accessContext,
            routeRoleId,
            "Operators",
            ctx.RequestAborted),
            Times.Once);

        f.Roles.Verify(x => x.RenameAsync(
            It.IsAny<AccessContext>(),
            bodyRoleId,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // Fixture
    // =========================================================

    private sealed class Fixture
    {
        public UserKey UserKey { get; } = UserKey.New();

        public Mock<IAuthFlowContextAccessor> AuthFlow { get; }
            = new(MockBehavior.Strict);

        public Mock<IAuthorizationService> Authorization { get; }
            = new(MockBehavior.Strict);

        public Mock<IUserRoleService> UserRoles { get; }
            = new(MockBehavior.Strict);

        public Mock<IRoleService> Roles { get; }
            = new(MockBehavior.Strict);

        public Mock<IAccessContextFactory> AccessContextFactory { get; }
            = new(MockBehavior.Strict);

        public AuthorizationEndpointHandler Sut { get; }

        public Fixture(bool isAuthenticated = true)
        {
            var flow = AuthFlowTestFactory.New(
                isAuthenticated: isAuthenticated,
                userKey: isAuthenticated ? UserKey : null);

            AuthFlow
                .SetupGet(x => x.Current)
                .Returns(flow);

            Sut = new AuthorizationEndpointHandler(
                AuthFlow.Object,
                Authorization.Object,
                UserRoles.Object,
                Roles.Object,
                AccessContextFactory.Object);
        }

        public DefaultHttpContext Http()
        {
            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            return ctx;
        }

        public DefaultHttpContext Json<T>(T value)
        {
            var ctx = Http();

            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);

            ctx.Request.ContentType = "application/json";
            ctx.Request.Body = new MemoryStream(bytes);
            ctx.Request.ContentLength = bytes.Length;

            return ctx;
        }

        public AccessContext AccessContext(string action)
            => TestAccessContext.WithAction(action);

        public void SetupAccessContext(
            string action,
            string resource,
            string? resourceId,
            AccessContext result)
        {
            AccessContextFactory
                .Setup(x => x.CreateAsync(
                    It.IsAny<AuthFlowContext>(),
                    action,
                    resource,
                    resourceId,
                    It.IsAny<IDictionary<string, object>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }
    }

    private static PagedResult<UserRoleInfo> EmptyRoles()
        => new(
            [],
            0,
            1,
            250,
            null,
            false);

    private static void AssertStatusCode(
    IResult result,
    int expectedStatusCode)
    {
        result.Should().BeAssignableTo<IStatusCodeHttpResult>();

        var statusResult = (IStatusCodeHttpResult)result;

        statusResult.StatusCode.Should().Be(expectedStatusCode);
    }
}