using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using System.Text;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class SessionEndpointHandlerTests
{
    // ---------------------------------------------------------------------
    // Authentication boundary
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetMyChainsAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var flow = CreateUnauthenticatedFlow();
        var fixture = CreateFixture(flow);

        var result = await fixture.Sut.GetMyChainsAsync(fixture.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        fixture.AccessFactory.VerifyNoOtherCalls();
        fixture.Sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RevokeMyChainAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var flow = CreateUnauthenticatedFlow();
        var fixture = CreateFixture(flow);

        var result = await fixture.Sut.RevokeMyChainAsync(
            SessionChainId.New(),
            fixture.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        fixture.AccessFactory.VerifyNoOtherCalls();
        fixture.Sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUserChainsAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var flow = CreateUnauthenticatedFlow();
        var fixture = CreateFixture(flow);

        var result = await fixture.Sut.GetUserChainsAsync(
            UserKey.New(),
            fixture.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        fixture.AccessFactory.VerifyNoOtherCalls();
        fixture.Sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RevokeRootAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var flow = CreateUnauthenticatedFlow();
        var fixture = CreateFixture(flow);

        var result = await fixture.Sut.RevokeRootAsync(
            UserKey.New(),
            fixture.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        fixture.AccessFactory.VerifyNoOtherCalls();
        fixture.Sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RevokeOtherChainsAsync_WhenSessionIsMissing_ReturnsUnauthorized()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();
        flow.Session.Should().BeNull();

        var fixture = CreateFixture(flow);

        var result = await fixture.Sut.RevokeOtherChainsAsync(
            fixture.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        fixture.AccessFactory.VerifyNoOtherCalls();
        fixture.Sessions.VerifyNoOtherCalls();
    }

    // ---------------------------------------------------------------------
    // Self endpoints
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetMyChainsAsync_UsesSelfActionAndCurrentUser()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();
        var user = flow.UserKey!.Value;
        var fixture = CreateFixture(flow);

        SetJsonBody(
            fixture.HttpContext,
            """
            {
                "pageNumber": 2,
                "pageSize": 25
            }
            """);

        var access = CreateAccess(flow, user, UAuthActions.Sessions.ListChainsSelf);

        PageRequest? capturedRequest = null;

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                flow,
                UAuthActions.Sessions.ListChainsSelf,
                "sessions",
                user.Value,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        var expected = new PagedResult<SessionChainSummary>(
            [],
            0,
            1,
            250,
            null,
            false);

        fixture.Sessions
            .Setup(x => x.GetUserChainsAsync(
                access,
                user,
                It.IsAny<PageRequest>(),
                fixture.HttpContext.RequestAborted))
            .Callback<AccessContext, UserKey, PageRequest, CancellationToken>(
                (_, _, request, _) => capturedRequest = request)
            .ReturnsAsync(expected);

        var result = await fixture.Sut.GetMyChainsAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<PagedResult<SessionChainSummary>>>()
            .Subject;

        ok.Value.Should().BeSameAs(expected);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.PageNumber.Should().Be(2);
        capturedRequest.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task GetMyChainDetailAsync_ForwardsChainIdAndUsesGetChainSelf()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();
        var user = flow.UserKey!.Value;
        var chainId = SessionChainId.New();
        var fixture = CreateFixture(flow);

        var access = CreateAccess(
            flow,
            user,
            UAuthActions.Sessions.GetChainSelf);

        var expected = new SessionChainDetail
        {
            ChainId = chainId
        };

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                flow,
                UAuthActions.Sessions.GetChainSelf,
                "sessions",
                user.Value,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        fixture.Sessions
            .Setup(x => x.GetUserChainDetailAsync(
                access,
                user,
                chainId,
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(expected);

        var result = await fixture.Sut.GetMyChainDetailAsync(
            chainId,
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<SessionChainDetail>>()
            .Subject;

        ok.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task RevokeMyChainAsync_ForwardsChainIdAndUsesRevokeChainSelf()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();
        var user = flow.UserKey!.Value;
        var chainId = SessionChainId.New();
        var fixture = CreateFixture(flow);

        var access = CreateAccess(
            flow,
            user,
            UAuthActions.Sessions.RevokeChainSelf);

        var expected = new RevokeResult
        {
            CurrentChain = true,
            RootRevoked = false
        };

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                flow,
                UAuthActions.Sessions.RevokeChainSelf,
                "sessions",
                user.Value,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        fixture.Sessions
            .Setup(x => x.RevokeUserChainAsync(
                access,
                user,
                chainId,
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(expected);

        var result = await fixture.Sut.RevokeMyChainAsync(
            chainId,
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<RevokeResult>>()
            .Subject;

        ok.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task RevokeAllMyChainsAsync_UsesSelfActionAndDoesNotExcludeChain()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();
        var user = flow.UserKey!.Value;
        var fixture = CreateFixture(flow);

        var access = CreateAccess(
            flow,
            user,
            UAuthActions.Sessions.RevokeAllChainsSelf);

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                flow,
                UAuthActions.Sessions.RevokeAllChainsSelf,
                "sessions",
                user.Value,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        fixture.Sessions
            .Setup(x => x.RevokeAllChainsAsync(
                access,
                user,
                null,
                fixture.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await fixture.Sut.RevokeAllMyChainsAsync(
            fixture.HttpContext);

        result.Should().BeOfType<Ok>();

        fixture.Sessions.Verify(
            x => x.RevokeAllChainsAsync(
                access,
                user,
                null,
                fixture.HttpContext.RequestAborted),
            Times.Once);
    }

    // ---------------------------------------------------------------------
    // Admin endpoints
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetUserChainsAsync_UsesAdminActionAndTargetUser()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();
        var targetUser = UserKey.New();
        var fixture = CreateFixture(flow);

        SetJsonBody(
            fixture.HttpContext,
            """
            {
                "pageNumber": 2,
                "pageSize": 25
            }
            """);

        var access = CreateAccess(
            flow,
            targetUser,
            UAuthActions.Sessions.ListChainsAdmin);

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                flow,
                UAuthActions.Sessions.ListChainsAdmin,
                "sessions",
                targetUser.Value,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        var expected = new PagedResult<SessionChainSummary>(
            [],
            0,
            2,
            25,
            null,
            false);

        fixture.Sessions
            .Setup(x => x.GetUserChainsAsync(
                access,
                targetUser,
                It.IsAny<PageRequest>(),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(expected);

        var result = await fixture.Sut.GetUserChainsAsync(
            targetUser,
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<PagedResult<SessionChainSummary>>>()
            .Subject;

        ok.Value.Should().BeSameAs(expected);

        fixture.AccessFactory.Verify(
            x => x.CreateAsync(
                flow,
                UAuthActions.Sessions.ListChainsAdmin,
                "sessions",
                targetUser.Value,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUserChainDetailAsync_UsesAdminActionAndForwardsTargetIds()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();
        var targetUser = UserKey.New();
        var chainId = SessionChainId.New();
        var fixture = CreateFixture(flow);

        var access = CreateAccess(
            flow,
            targetUser,
            UAuthActions.Sessions.GetChainAdmin);

        var expected = new SessionChainDetail
        {
            ChainId = chainId
        };

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                flow,
                UAuthActions.Sessions.GetChainAdmin,
                "sessions",
                targetUser.Value,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        fixture.Sessions
            .Setup(x => x.GetUserChainDetailAsync(
                access,
                targetUser,
                chainId,
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(expected);

        var result = await fixture.Sut.GetUserChainDetailAsync(
            targetUser,
            chainId,
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<SessionChainDetail>>()
            .Subject;

        ok.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task RevokeUserSessionAsync_UsesAdminActionAndForwardsSessionId()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();
        var targetUser = UserKey.New();
        var sessionId = TestIds.Session("test-session");
        var fixture = CreateFixture(flow);

        var access = CreateAccess(
            flow,
            targetUser,
            UAuthActions.Sessions.RevokeSessionAdmin);

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                flow,
                UAuthActions.Sessions.RevokeSessionAdmin,
                "sessions",
                targetUser.Value,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        fixture.Sessions
            .Setup(x => x.RevokeUserSessionAsync(
                access,
                targetUser,
                sessionId,
                fixture.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await fixture.Sut.RevokeUserSessionAsync(
            targetUser,
            sessionId,
            fixture.HttpContext);

        result.Should().BeOfType<Ok>();

        fixture.Sessions.Verify(
            x => x.RevokeUserSessionAsync(
                access,
                targetUser,
                sessionId,
                fixture.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task RevokeUserChainAsync_UsesAdminActionAndForwardsChainId()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();
        var targetUser = UserKey.New();
        var chainId = SessionChainId.New();
        var fixture = CreateFixture(flow);

        var access = CreateAccess(
            flow,
            targetUser,
            UAuthActions.Sessions.RevokeChainAdmin);

        var expected = new RevokeResult
        {
            CurrentChain = false,
            RootRevoked = false
        };

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                flow,
                UAuthActions.Sessions.RevokeChainAdmin,
                "sessions",
                targetUser.Value,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        fixture.Sessions
            .Setup(x => x.RevokeUserChainAsync(
                access,
                targetUser,
                chainId,
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(expected);

        var result = await fixture.Sut.RevokeUserChainAsync(
            targetUser,
            chainId,
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<RevokeResult>>()
            .Subject;

        ok.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task RevokeAllChainsAsync_UsesAdminActionAndForwardsExceptChain()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();
        var targetUser = UserKey.New();
        var exceptChainId = SessionChainId.New();
        var fixture = CreateFixture(flow);

        var access = CreateAccess(
            flow,
            targetUser,
            UAuthActions.Sessions.RevokeAllChainsAdmin);

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                flow,
                UAuthActions.Sessions.RevokeAllChainsAdmin,
                "sessions",
                targetUser.Value,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        fixture.Sessions
            .Setup(x => x.RevokeAllChainsAsync(
                access,
                targetUser,
                exceptChainId,
                fixture.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await fixture.Sut.RevokeAllChainsAsync(
            targetUser,
            exceptChainId,
            fixture.HttpContext);

        result.Should().BeOfType<Ok>();

        fixture.Sessions.Verify(
            x => x.RevokeAllChainsAsync(
                access,
                targetUser,
                exceptChainId,
                fixture.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task RevokeRootAsync_UsesAdminActionAndForwardsTargetUser()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();
        var targetUser = UserKey.New();
        var fixture = CreateFixture(flow);

        var access = CreateAccess(
            flow,
            targetUser,
            UAuthActions.Sessions.RevokeRootAdmin);

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                flow,
                UAuthActions.Sessions.RevokeRootAdmin,
                "sessions",
                targetUser.Value,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        fixture.Sessions
            .Setup(x => x.RevokeRootAsync(
                access,
                targetUser,
                fixture.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await fixture.Sut.RevokeRootAsync(
            targetUser,
            fixture.HttpContext);

        result.Should().BeOfType<Ok>();

        fixture.Sessions.Verify(
            x => x.RevokeRootAsync(
                access,
                targetUser,
                fixture.HttpContext.RequestAborted),
            Times.Once);
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static Fixture CreateFixture(AuthFlowContext flow)
    {
        var authFlow =
            new Mock<IAuthFlowContextAccessor>(MockBehavior.Strict);

        var accessFactory =
            new Mock<IAccessContextFactory>(MockBehavior.Strict);

        var sessions =
            new Mock<ISessionApplicationService>(MockBehavior.Strict);

        authFlow
            .SetupGet(x => x.Current)
            .Returns(flow);

        var httpContext = new DefaultHttpContext();

        var sut = new SessionEndpointHandler(
            authFlow.Object,
            accessFactory.Object,
            sessions.Object);

        return new Fixture(
            sut,
            accessFactory,
            sessions,
            httpContext);
    }

    private static AccessContext CreateAccess(
        AuthFlowContext flow,
        UserKey targetUser,
        string action)
    {
        return new AccessContext(
            actorUserKey: flow.UserKey,
            actorTenant: flow.Tenant,
            isAuthenticated: flow.IsAuthenticated,
            isSystemActor: false,
            actorChainId: flow.Session?.ChainId,
            resource: "sessions",
            targetUserKey: targetUser,
            resourceTenant: flow.Tenant,
            action: action,
            attributes:
                new Dictionary<string, object>());
    }

    private static AuthFlowContext CreateUnauthenticatedFlow()
    {
        var authenticated = AuthFlowTestFactory.LoginSuccess();

        return new AuthFlowContext(
            flowType: authenticated.FlowType,
            clientProfile: authenticated.ClientProfile,
            effectiveMode: authenticated.EffectiveMode,
            device: authenticated.Device,
            tenantKey: authenticated.Tenant,
            isAuthenticated: false,
            userKey: null,
            session: null,
            originalOptions: authenticated.OriginalOptions,
            effectiveOptions: authenticated.EffectiveOptions,
            response: authenticated.Response,
            primaryTokenKind: authenticated.PrimaryTokenKind,
            returnUrlInfo: authenticated.ReturnUrlInfo);
    }

    private sealed record Fixture(
        SessionEndpointHandler Sut,
        Mock<IAccessContextFactory> AccessFactory,
        Mock<ISessionApplicationService> Sessions,
        DefaultHttpContext HttpContext);

    private static void SetJsonBody(HttpContext context, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);

        context.Request.ContentType = "application/json";
        context.Request.ContentLength = bytes.Length;
        context.Request.Body = new MemoryStream(bytes);
    }
}