using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using System.Text;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit.Users;

public sealed class UserEndpointHandlerTests
{
    // ============================================================
    // QueryUsers
    // ============================================================

    [Fact]
    public async Task QueryUsersAsync_WhenUnauthenticated_ReturnsUnauthorized_AndDoesNotCallDownstream()
    {
        var f = CreateFixture(authenticated: false);

        var result = await f.Sut.QueryUsersAsync(f.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task QueryUsersAsync_WhenAuthenticated_UsesQueryAdminAccessContext()
    {
        var f = CreateFixture();

        var request = new UserQuery();

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.Users.QueryAdmin,
                "users",
                null,
                null,
                default))
            .ReturnsAsync(accessContext);

        var expected = new PagedResult<UserSummary>(
            Array.Empty<UserSummary>(),
            0,
            1,
            20,
            null,
            false);

        f.Users
            .Setup(x => x.QueryUsersAsync(
                accessContext,
                It.IsAny<UserQuery>(),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(expected);

        var result = await f.Sut.QueryUsersAsync(f.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<PagedResult<UserSummary>>>()
            .Subject;

        ok.Value.Should().BeSameAs(expected);

        f.AccessContextFactory.Verify(x => x.CreateAsync(
            f.Flow,
            UAuthActions.Users.QueryAdmin,
            "users",
            null,
            null,
            default),
            Times.Once);

        f.Users.Verify(x => x.QueryUsersAsync(
            accessContext,
            It.IsAny<UserQuery>(),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    // ============================================================
    // Create anonymous
    // ============================================================

    [Fact]
    public async Task CreateAsync_WhenUnauthenticated_AllowsAnonymousCreation()
    {
        var f = CreateFixture(authenticated: false);

        var request = CreateUserRequest();

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.Users.CreateAnonymous,
                "users",
                null,
                null,
                default))
            .ReturnsAsync(accessContext);

        var userKey = UserKey.New();
        var createResult = UserCreateResult.Success(userKey);

        f.Users
            .Setup(x => x.CreateUserAsync(
                accessContext,
                It.IsAny<CreateUserRequest>(),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(createResult);

        var result = await f.Sut.CreateAsync(f.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<UserCreateResult>>()
            .Subject;

        ok.Value.Should().BeSameAs(createResult);

        f.AccessContextFactory.Verify(x => x.CreateAsync(
            f.Flow,
            UAuthActions.Users.CreateAnonymous,
            "users",
            null,
            null,
            default),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCreationFails_ReturnsBadRequest()
    {
        var f = CreateFixture(authenticated: false);

        var request = CreateUserRequest();

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.Users.CreateAnonymous,
                "users",
                null,
                null,
                default))
            .ReturnsAsync(accessContext);

        var createResult = UserCreateResult.Failed("fail");

        f.Users
            .Setup(x => x.CreateUserAsync(
                accessContext,
                It.IsAny<CreateUserRequest>(),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(createResult);

        var result = await f.Sut.CreateAsync(f.HttpContext);

        var badRequest = result.Should()
            .BeOfType<BadRequest<UserCreateResult>>()
            .Subject;

        badRequest.Value.Should().BeSameAs(createResult);
    }

    // ============================================================
    // Create admin
    // ============================================================

    [Fact]
    public async Task CreateAdminAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = CreateFixture(authenticated: false);

        var result = await f.Sut.CreateAdminAsync(f.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAdminAsync_WhenAuthenticated_UsesCreateAdminAction()
    {
        var f = CreateFixture();

        SetJsonBody(
            f.HttpContext,
            CreateUserRequest());

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.Users.CreateAdmin,
                "users",
                null,
                null,
                default))
            .ReturnsAsync(accessContext);

        var createResult =
            UserCreateResult.Success(UserKey.New());

        f.Users
            .Setup(x => x.CreateUserAsync(
                accessContext,
                It.IsAny<CreateUserRequest>(),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(createResult);

        var result = await f.Sut.CreateAdminAsync(f.HttpContext);

        result.Should()
            .BeOfType<Ok<UserCreateResult>>();

        f.AccessContextFactory.Verify(x => x.CreateAsync(
            f.Flow,
            UAuthActions.Users.CreateAdmin,
            "users",
            null,
            null,
            default),
            Times.Once);
    }

    // ============================================================
    // Change status self
    // ============================================================

    [Fact]
    public async Task ChangeStatusSelfAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = CreateFixture(authenticated: false);

        var result =
            await f.Sut.ChangeStatusSelfAsync(f.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangeStatusSelfAsync_UsesSelfActionAndActorUserKey()
    {
        var f = CreateFixture();

        var request = new ChangeUserStatusSelfRequest
        {
            NewStatus = SelfAssignableUserStatus.SelfSuspended
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.Users.ChangeStatusSelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.ChangeUserStatusAsync(
                accessContext,
                It.IsAny<ChangeUserStatusSelfRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.ChangeStatusSelfAsync(f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.ChangeUserStatusAsync(
            accessContext,
            It.Is<ChangeUserStatusSelfRequest>(
                r => r.NewStatus == request.NewStatus),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    // ============================================================
    // Change status admin
    // ============================================================

    [Fact]
    public async Task ChangeStatusAdminAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = CreateFixture(authenticated: false);

        var targetUser = UserKey.New();

        var result = await f.Sut.ChangeStatusAdminAsync(
            targetUser,
            f.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangeStatusAdminAsync_UsesAdminActionAndTargetUserKey()
    {
        var f = CreateFixture();

        var targetUser = UserKey.New();

        var request = new ChangeUserStatusAdminRequest
        {
            NewStatus = AdminAssignableUserStatus.Suspended
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext =
            CreateAccessContext(f, targetUser);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.Users.ChangeStatusAdmin,
                "users",
                targetUser.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.ChangeUserStatusAsync(
                accessContext,
                It.IsAny<ChangeUserStatusAdminRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.ChangeStatusAdminAsync(
            targetUser,
            f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.ChangeUserStatusAsync(
            accessContext,
            It.Is<ChangeUserStatusAdminRequest>(
                r => r.NewStatus == request.NewStatus),
            f.HttpContext.RequestAborted),
            Times.Once);

        f.AccessContextFactory.Verify(x => x.CreateAsync(
            f.Flow,
            UAuthActions.Users.ChangeStatusAdmin,
            "users",
            targetUser.Value,
            null,
            default),
            Times.Once);
    }

    // ============================================================
    // Delete self
    // ============================================================

    [Fact]
    public async Task DeleteMeAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = CreateFixture(authenticated: false);

        var result =
            await f.Sut.DeleteMeAsync(f.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteMeAsync_UsesDeleteSelfAndActorUserKey()
    {
        var f = CreateFixture();

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.Users.DeleteSelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.DeleteMeAsync(
                accessContext,
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.DeleteMeAsync(f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.DeleteMeAsync(
            accessContext,
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    // ============================================================
    // Fixture
    // ============================================================

    private static Fixture CreateFixture(
        bool authenticated = true)
    {
        var flow = authenticated
            ? AuthFlowTestFactory.LoginSuccess()
            : AuthFlowTestFactory.New(isAuthenticated: false);

        var authFlow =
            new Mock<IAuthFlowContextAccessor>(
                MockBehavior.Strict);

        var accessContextFactory =
            new Mock<IAccessContextFactory>(
                MockBehavior.Strict);

        var users =
            new Mock<IUserApplicationService>(
                MockBehavior.Strict);

        authFlow
            .SetupGet(x => x.Current)
            .Returns(flow);

        var httpContext =
            new DefaultHttpContext();

        var cts =
            new CancellationTokenSource();

        httpContext.RequestAborted = cts.Token;

        var sut = new UserEndpointHandler(
            authFlow.Object,
            accessContextFactory.Object,
            users.Object);

        return new Fixture(
            sut,
            flow,
            accessContextFactory,
            users,
            httpContext);
    }

    private static AccessContext CreateAccessContext(Fixture f, UserKey? targetUserKey = null)
    {
        return new AccessContext(
            actorUserKey: f.Flow.UserKey,
            actorTenant: f.Flow.Tenant,
            isAuthenticated: f.Flow.IsAuthenticated,
            isSystemActor: false,
            actorChainId: f.Flow.Session?.ChainId,
            resource: "users",
            targetUserKey: targetUserKey ?? f.Flow.UserKey,
            resourceTenant: f.Flow.Tenant,
            action: "test",
            attributes: EmptyAttributes.Instance);
    }

    private static CreateUserRequest CreateUserRequest()
    {
        return new CreateUserRequest
        {
            UserName = "alice"
        };
    }

    private static void SetJsonBody<T>(HttpContext context, T value)
    {
        var json = JsonSerializer.Serialize(value);

        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));

        context.Request.ContentType = "application/json";

        context.Request.ContentLength = context.Request.Body.Length;
    }

    private sealed record Fixture(
        UserEndpointHandler Sut,
        AuthFlowContext Flow,
        Mock<IAccessContextFactory> AccessContextFactory,
        Mock<IUserApplicationService> Users,
        DefaultHttpContext HttpContext);
}