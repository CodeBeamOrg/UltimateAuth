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
    // Delete admin
    // ============================================================

    [Fact]
    public async Task DeleteAsync_UsesDeleteAdminAction_TargetUser_AndDeleteModeAttribute()
    {
        var f = CreateFixture();
        var targetUser = UserKey.New();

        var request = new DeleteUserRequest
        {
            Mode = DeleteMode.Hard
        };

        SetJsonBody(f.HttpContext, request);

        AccessContext? capturedContext = null;

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.Users.DeleteAdmin,
                "users",
                targetUser.Value,
                It.Is<IDictionary<string, object>>(a =>
                    a.ContainsKey("deleteMode") &&
                    (DeleteMode)a["deleteMode"] == DeleteMode.Hard),
                default))
            .ReturnsAsync((AuthFlowContext _,
                string _,
                string _,
                string? _,
                IDictionary<string, object>? _,
                CancellationToken _) =>
            {
                capturedContext = CreateAccessContext(f, targetUser);
                return capturedContext;
            });

        f.Users
            .Setup(x => x.DeleteUserAsync(
                It.IsAny<AccessContext>(),
                It.Is<DeleteUserRequest>(r => r.Mode == DeleteMode.Hard),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.DeleteAsync(
            targetUser,
            f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.DeleteUserAsync(
            It.IsAny<AccessContext>(),
            It.Is<DeleteUserRequest>(r => r.Mode == DeleteMode.Hard),
            f.HttpContext.RequestAborted),
            Times.Once);
    }


    // ============================================================
    // Profiles
    // ============================================================

    // ============================================================
    // Identifier mutation - Set primary
    // ============================================================

    [Fact]
    public async Task SetPrimaryUserIdentifierSelfAsync_UsesSetPrimarySelfAction()
    {
        var f = CreateFixture();

        var request = new SetPrimaryUserIdentifierRequest
        {
            Id = Guid.NewGuid()
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.SetPrimarySelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.SetPrimaryUserIdentifierAsync(
                accessContext,
                It.IsAny<SetPrimaryUserIdentifierRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.SetPrimaryUserIdentifierSelfAsync(
                f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.SetPrimaryUserIdentifierAsync(
            accessContext,
            It.Is<SetPrimaryUserIdentifierRequest>(
                r => r.Id == request.Id),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task SetPrimaryUserIdentifierAdminAsync_UsesSetPrimaryAdminAction()
    {
        var f = CreateFixture();
        var targetUser = UserKey.New();

        var request = new SetPrimaryUserIdentifierRequest
        {
            Id = Guid.NewGuid()
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f, targetUser);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.SetPrimaryAdmin,
                "users",
                targetUser.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.SetPrimaryUserIdentifierAsync(
                accessContext,
                It.IsAny<SetPrimaryUserIdentifierRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.SetPrimaryUserIdentifierAdminAsync(
                targetUser,
                f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.SetPrimaryUserIdentifierAsync(
            accessContext,
            It.Is<SetPrimaryUserIdentifierRequest>(
                r => r.Id == request.Id),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    // ============================================================
    // Identifier mutation - Unset primary
    // ============================================================

    [Fact]
    public async Task UnsetPrimaryUserIdentifierSelfAsync_UsesUnsetPrimarySelfAction()
    {
        var f = CreateFixture();

        var request = new UnsetPrimaryUserIdentifierRequest
        {
            Id = Guid.NewGuid()
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.UnsetPrimarySelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.UnsetPrimaryUserIdentifierAsync(
                accessContext,
                It.IsAny<UnsetPrimaryUserIdentifierRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.UnsetPrimaryUserIdentifierSelfAsync(
                f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.UnsetPrimaryUserIdentifierAsync(
            accessContext,
            It.Is<UnsetPrimaryUserIdentifierRequest>(
                r => r.Id == request.Id),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task UnsetPrimaryUserIdentifierAdminAsync_UsesUnsetPrimaryAdminAction()
    {
        var f = CreateFixture();
        var targetUser = UserKey.New();

        var request = new UnsetPrimaryUserIdentifierRequest
        {
            Id = Guid.NewGuid()
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f, targetUser);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.UnsetPrimaryAdmin,
                "users",
                targetUser.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.UnsetPrimaryUserIdentifierAsync(
                accessContext,
                It.IsAny<UnsetPrimaryUserIdentifierRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.UnsetPrimaryUserIdentifierAdminAsync(
                targetUser,
                f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.UnsetPrimaryUserIdentifierAsync(
            accessContext,
            It.Is<UnsetPrimaryUserIdentifierRequest>(
                r => r.Id == request.Id),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task GetMeAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = CreateFixture(authenticated: false);

        var result = await f.Sut.GetMeAsync(f.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IdentifierExistsSelfAsync_UsesWithinUserScope()
    {
        var f = CreateFixture();

        var request = new IdentifierExistsRequest
        {
            Type = UserIdentifierType.Email,
            Value = "alice@example.com"
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.GetSelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.UserIdentifierExistsAsync(
                accessContext,
                UserIdentifierType.Email,
                "alice@example.com",
                IdentifierExistenceScope.WithinUser,
                f.HttpContext.RequestAborted))
            .ReturnsAsync(true);

        var result =
            await f.Sut.IdentifierExistsSelfAsync(f.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<IdentifierExistsResponse>>()
            .Subject;

        ok.Value!.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task IdentifierExistsAdminAsync_UsesTenantAnyScope()
    {
        var f = CreateFixture();
        var targetUser = UserKey.New();

        var request = new IdentifierExistsRequest
        {
            Type = UserIdentifierType.Email,
            Value = "alice@example.com"
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f, targetUser);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.GetAdmin,
                "users",
                targetUser.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.UserIdentifierExistsAsync(
                accessContext,
                UserIdentifierType.Email,
                "alice@example.com",
                IdentifierExistenceScope.TenantAny,
                f.HttpContext.RequestAborted))
            .ReturnsAsync(true);

        var result = await f.Sut.IdentifierExistsAdminAsync(
            targetUser,
            f.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<IdentifierExistsResponse>>()
            .Subject;

        ok.Value!.Exists.Should().BeTrue();
    }


    // ============================================================
    // Identifier mutation - Add
    // ============================================================

    [Fact]
    public async Task AddUserIdentifierSelfAsync_UsesAddSelfAction()
    {
        var f = CreateFixture();

        var request = new AddUserIdentifierRequest
        {
            Type = UserIdentifierType.Email,
            Value = "alice@example.com"
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.AddSelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.AddUserIdentifierAsync(
                accessContext,
                It.IsAny<AddUserIdentifierRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.AddUserIdentifierSelfAsync(f.HttpContext);

        result.Should().BeOfType<Ok>();
    }

    [Fact]
    public async Task AddUserIdentifierAdminAsync_UsesAddAdminAction()
    {
        var f = CreateFixture();
        var targetUser = UserKey.New();

        var request = new AddUserIdentifierRequest
        {
            Type = UserIdentifierType.Email,
            Value = "alice@example.com"
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f, targetUser);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.AddAdmin,
                "users",
                targetUser.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.AddUserIdentifierAsync(
                accessContext,
                It.IsAny<AddUserIdentifierRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.AddUserIdentifierAdminAsync(
            targetUser,
            f.HttpContext);

        result.Should().BeOfType<Ok>();
    }


    // ============================================================
    // Identifier mutation - Update
    // ============================================================

    [Fact]
    public async Task UpdateUserIdentifierSelfAsync_UsesUpdateSelfAction()
    {
        var f = CreateFixture();

        var request = new UpdateUserIdentifierRequest
        {
            Id = Guid.NewGuid(),
            NewValue = "new@example.com"
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.UpdateSelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.UpdateUserIdentifierAsync(
                accessContext,
                It.IsAny<UpdateUserIdentifierRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.UpdateUserIdentifierSelfAsync(f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.UpdateUserIdentifierAsync(
            accessContext,
            It.Is<UpdateUserIdentifierRequest>(r =>
                r.Id == request.Id &&
                r.NewValue == request.NewValue),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserIdentifierAdminAsync_UsesUpdateAdminAction()
    {
        var f = CreateFixture();
        var targetUser = UserKey.New();

        var request = new UpdateUserIdentifierRequest
        {
            Id = Guid.NewGuid(),
            NewValue = "new@example.com"
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f, targetUser);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.UpdateAdmin,
                "users",
                targetUser.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.UpdateUserIdentifierAsync(
                accessContext,
                It.IsAny<UpdateUserIdentifierRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.UpdateUserIdentifierAdminAsync(
            targetUser,
            f.HttpContext);

        result.Should().BeOfType<Ok>();
    }

    // ============================================================
    // Identifier queries
    // ============================================================

    [Fact]
    public async Task GetMyIdentifiersAsync_UsesGetSelfAction()
    {
        var f = CreateFixture();

        SetJsonBody(f.HttpContext, new UserIdentifierQuery());

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.GetSelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        var expected = new PagedResult<UserIdentifierInfo>(
            Array.Empty<UserIdentifierInfo>(),
            0,
            1,
            20,
            null,
            false);

        f.Users
            .Setup(x => x.GetIdentifiersByUserAsync(
                accessContext,
                It.IsAny<UserIdentifierQuery>(),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(expected);

        var result =
            await f.Sut.GetMyIdentifiersAsync(f.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<PagedResult<UserIdentifierInfo>>>()
            .Subject;

        ok.Value.Should().BeSameAs(expected);

        f.AccessContextFactory.Verify(x => x.CreateAsync(
            f.Flow,
            UAuthActions.UserIdentifiers.GetSelf,
            "users",
            f.Flow.UserKey!.Value.Value,
            null,
            default),
            Times.Once);

        f.Users.Verify(x => x.GetIdentifiersByUserAsync(
            accessContext,
            It.IsAny<UserIdentifierQuery>(),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task GetUserIdentifiersAsync_UsesGetAdminActionAndTargetUserKey()
    {
        var f = CreateFixture();
        var targetUser = UserKey.New();

        SetJsonBody(f.HttpContext, new UserIdentifierQuery());

        var accessContext = CreateAccessContext(f, targetUser);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.GetAdmin,
                "users",
                targetUser.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        var expected = new PagedResult<UserIdentifierInfo>(
            Array.Empty<UserIdentifierInfo>(),
            0,
            1,
            20,
            null,
            false);

        f.Users
            .Setup(x => x.GetIdentifiersByUserAsync(
                accessContext,
                It.IsAny<UserIdentifierQuery>(),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(expected);

        var result = await f.Sut.GetUserIdentifiersAsync(
            targetUser,
            f.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<PagedResult<UserIdentifierInfo>>>()
            .Subject;

        ok.Value.Should().BeSameAs(expected);

        f.AccessContextFactory.Verify(x => x.CreateAsync(
            f.Flow,
            UAuthActions.UserIdentifiers.GetAdmin,
            "users",
            targetUser.Value,
            null,
            default),
            Times.Once);
    }


    // ============================================================
    // Identifier mutation - Verify
    // ============================================================

    [Fact]
    public async Task VerifyUserIdentifierSelfAsync_UsesVerifySelfAction()
    {
        var f = CreateFixture();

        var request = new VerifyUserIdentifierRequest
        {
            Id = Guid.NewGuid()
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.VerifySelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.VerifyUserIdentifierAsync(
                accessContext,
                It.IsAny<VerifyUserIdentifierRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.VerifyUserIdentifierSelfAsync(
                f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.VerifyUserIdentifierAsync(
            accessContext,
            It.Is<VerifyUserIdentifierRequest>(
                r => r.Id == request.Id),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task VerifyUserIdentifierAdminAsync_UsesVerifyAdminActionAndTargetUserKey()
    {
        var f = CreateFixture();
        var targetUser = UserKey.New();

        var request = new VerifyUserIdentifierRequest
        {
            Id = Guid.NewGuid()
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f, targetUser);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.VerifyAdmin,
                "users",
                targetUser.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.VerifyUserIdentifierAsync(
                accessContext,
                It.IsAny<VerifyUserIdentifierRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.VerifyUserIdentifierAdminAsync(
                targetUser,
                f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.VerifyUserIdentifierAsync(
            accessContext,
            It.Is<VerifyUserIdentifierRequest>(
                r => r.Id == request.Id),
            f.HttpContext.RequestAborted),
            Times.Once);
    }


    // ============================================================
    // Identifier mutation - Delete
    // ============================================================

    [Fact]
    public async Task DeleteUserIdentifierSelfAsync_UsesDeleteSelfActionAndPassesRequest()
    {
        var f = CreateFixture();

        var request = new DeleteUserIdentifierRequest
        {
            Id = Guid.NewGuid(),
            Mode = DeleteMode.Soft
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.DeleteSelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.DeleteUserIdentifierAsync(
                accessContext,
                It.IsAny<DeleteUserIdentifierRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.DeleteUserIdentifierSelfAsync(
                f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.DeleteUserIdentifierAsync(
            accessContext,
            It.Is<DeleteUserIdentifierRequest>(r =>
                r.Id == request.Id &&
                r.Mode == DeleteMode.Soft),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task DeleteUserIdentifierAdminAsync_UsesDeleteAdminActionAndTargetUserKey()
    {
        var f = CreateFixture();
        var targetUser = UserKey.New();

        var request = new DeleteUserIdentifierRequest
        {
            Id = Guid.NewGuid(),
            Mode = DeleteMode.Hard
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f, targetUser);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserIdentifiers.DeleteAdmin,
                "users",
                targetUser.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.DeleteUserIdentifierAsync(
                accessContext,
                It.IsAny<DeleteUserIdentifierRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.DeleteUserIdentifierAdminAsync(
                targetUser,
                f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.DeleteUserIdentifierAsync(
            accessContext,
            It.Is<DeleteUserIdentifierRequest>(r =>
                r.Id == request.Id &&
                r.Mode == DeleteMode.Hard),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    // ============================================================
    // Profiles - Get
    // ============================================================

    [Fact]
    public async Task GetMeAsync_UsesGetSelfActionAndActorUserKey()
    {
        var f = CreateFixture();

        var request = new GetProfileRequest
        {
            ProfileKey = ProfileKey.Default
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserProfiles.GetSelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.GetMeAsync(
                accessContext,
                ProfileKey.Default,
                f.HttpContext.RequestAborted))
            .ReturnsAsync((UserView)null!);

        var result = await f.Sut.GetMeAsync(f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.AccessContextFactory.Verify(x => x.CreateAsync(
            f.Flow,
            UAuthActions.UserProfiles.GetSelf,
            "users",
            f.Flow.UserKey!.Value.Value,
            null,
            default),
            Times.Once);

        f.Users.Verify(x => x.GetMeAsync(
            accessContext,
            ProfileKey.Default,
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task GetUserAsync_UsesGetAdminActionAndTargetUserKey()
    {
        var f = CreateFixture();
        var targetUser = UserKey.New();

        var request = new GetProfileRequest
        {
            ProfileKey = ProfileKey.Default
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f, targetUser);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserProfiles.GetAdmin,
                "users",
                targetUser.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.GetUserProfileAsync(
                accessContext,
                ProfileKey.Default,
                f.HttpContext.RequestAborted))
            .ReturnsAsync((UserView)null!);

        var result = await f.Sut.GetUserAsync(
            targetUser,
            f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.AccessContextFactory.Verify(x => x.CreateAsync(
            f.Flow,
            UAuthActions.UserProfiles.GetAdmin,
            "users",
            targetUser.Value,
            null,
            default),
            Times.Once);

        f.Users.Verify(x => x.GetUserProfileAsync(
            accessContext,
            ProfileKey.Default,
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    // ============================================================
    // Profiles - Update
    // ============================================================

    [Fact]
    public async Task UpdateMeAsync_UsesUpdateSelfActionAndPassesRequest()
    {
        var f = CreateFixture();

        var request = new UpdateProfileRequest
        {
            ProfileKey = ProfileKey.Default,
            DisplayName = "Alice"
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserProfiles.UpdateSelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.UpdateUserProfileAsync(
                accessContext,
                It.IsAny<UpdateProfileRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.UpdateMeAsync(f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.UpdateUserProfileAsync(
            accessContext,
            It.Is<UpdateProfileRequest>(r =>
                r.ProfileKey == ProfileKey.Default &&
                r.DisplayName == "Alice"),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_UsesUpdateAdminActionAndTargetUserKey()
    {
        var f = CreateFixture();
        var targetUser = UserKey.New();

        var request = new UpdateProfileRequest
        {
            ProfileKey = ProfileKey.Default,
            DisplayName = "Alice Admin"
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f, targetUser);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserProfiles.UpdateAdmin,
                "users",
                targetUser.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.UpdateUserProfileAsync(
                accessContext,
                It.IsAny<UpdateProfileRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await f.Sut.UpdateUserAsync(
            targetUser,
            f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.UpdateUserProfileAsync(
            accessContext,
            It.Is<UpdateProfileRequest>(r =>
                r.ProfileKey == ProfileKey.Default &&
                r.DisplayName == "Alice Admin"),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    // ============================================================
    // Profiles - Create
    // ============================================================

    [Fact]
    public async Task CreateProfileSelfAsync_UsesCreateSelfActionAndPassesRequest()
    {
        var f = CreateFixture();

        var request = new CreateProfileRequest
        {
            ProfileKey = ProfileKey.Default,
            DisplayName = "Alice"
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserProfiles.CreateSelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.CreateProfileAsync(
                accessContext,
                It.IsAny<CreateProfileRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.CreateProfileSelfAsync(
                f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.CreateProfileAsync(
            accessContext,
            It.Is<CreateProfileRequest>(r =>
                r.ProfileKey == ProfileKey.Default &&
                r.DisplayName == "Alice"),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task CreateProfileAdminAsync_UsesCreateAdminActionAndTargetUserKey()
    {
        var f = CreateFixture();
        var targetUser = UserKey.New();

        var request = new CreateProfileRequest
        {
            ProfileKey = ProfileKey.Default,
            DisplayName = "Alice"
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f, targetUser);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserProfiles.CreateAdmin,
                "users",
                targetUser.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.CreateProfileAsync(
                accessContext,
                It.IsAny<CreateProfileRequest>(),
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.CreateProfileAdminAsync(
                targetUser,
                f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.CreateProfileAsync(
            accessContext,
            It.Is<CreateProfileRequest>(r =>
                r.ProfileKey == ProfileKey.Default &&
                r.DisplayName == "Alice"),
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    // ============================================================
    // Profiles - Delete
    // ============================================================

    [Fact]
    public async Task DeleteProfileSelfAsync_UsesDeleteSelfActionAndProfileKey()
    {
        var f = CreateFixture();

        var request = new DeleteProfileRequest
        {
            ProfileKey = ProfileKey.Default
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserProfiles.DeleteSelf,
                "users",
                f.Flow.UserKey!.Value.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.DeleteProfileAsync(
                accessContext,
                ProfileKey.Default,
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.DeleteProfileSelfAsync(
                f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.DeleteProfileAsync(
            accessContext,
            ProfileKey.Default,
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task DeleteProfileAdminAsync_UsesDeleteAdminActionAndTargetUserKey()
    {
        var f = CreateFixture();
        var targetUser = UserKey.New();

        var request = new DeleteProfileRequest
        {
            ProfileKey = ProfileKey.Default
        };

        SetJsonBody(f.HttpContext, request);

        var accessContext = CreateAccessContext(f, targetUser);

        f.AccessContextFactory
            .Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.UserProfiles.DeleteAdmin,
                "users",
                targetUser.Value,
                null,
                default))
            .ReturnsAsync(accessContext);

        f.Users
            .Setup(x => x.DeleteProfileAsync(
                accessContext,
                ProfileKey.Default,
                f.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result =
            await f.Sut.DeleteProfileAdminAsync(
                targetUser,
                f.HttpContext);

        result.Should().BeOfType<Ok>();

        f.Users.Verify(x => x.DeleteProfileAsync(
            accessContext,
            ProfileKey.Default,
            f.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = CreateFixture(authenticated: false);
        var targetUser = UserKey.New();

        var result = await f.Sut.DeleteAsync(
            targetUser,
            f.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteUserIdentifierSelfAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = CreateFixture(authenticated: false);

        var result =
            await f.Sut.DeleteUserIdentifierSelfAsync(
                f.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Users.VerifyNoOtherCalls();
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