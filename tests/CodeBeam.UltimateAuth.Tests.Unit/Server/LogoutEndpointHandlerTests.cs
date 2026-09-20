using System.Text;
using System.Text.Json;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class LogoutEndpointHandlerTests
{
    // =====================================================================
    // LogoutAsync
    // =====================================================================

    [Fact]
    public async Task LogoutAsync_WhenSessionIsMissing_DoesNotCallFlowLogout()
    {
        var fixture = CreateFixture();

        SetupNoRedirect(fixture);

        var result = await fixture.Sut.LogoutAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<LogoutResponse>>()
            .Subject;

        ok.Value.Should().NotBeNull();
        ok.Value!.Success.Should().BeTrue();

        fixture.FlowService.Verify(
            x => x.LogoutAsync(
                It.IsAny<LogoutRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_WhenSessionExists_ForwardsCurrentSessionId()
    {
        var sessionId = TestIds.Session("logout-current-session");

        var session = new SessionSecurityContext
        {
            UserKey = UserKey.New(),
            SessionId = sessionId,
            State = SessionState.Active,
            ChainId = SessionChainId.New(),
            BoundDeviceId = null
        };

        var flow = CreateFlowWithSession(session);
        var fixture = CreateFixture(flow);

        fixture.FlowService
            .Setup(x => x.LogoutAsync(
                It.Is<LogoutRequest>(
                    request => request.SessionId == sessionId),
                fixture.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        SetupNoRedirect(fixture);

        var result = await fixture.Sut.LogoutAsync(
            fixture.HttpContext);

        result.Should().BeOfType<Ok<LogoutResponse>>();

        fixture.FlowService.Verify(x => x.LogoutAsync(
            It.Is<LogoutRequest>(
                request => request.SessionId == sessionId),
            fixture.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WhenRedirectIsEnabled_ReturnsRedirect()
    {
        var fixture = CreateFixture();

        fixture.RedirectResolver
            .Setup(x => x.ResolveSuccess(
                fixture.Flow,
                fixture.HttpContext))
            .Returns(new RedirectDecision(
                enabled: true,
                targetUrl: "/after-logout"));

        var result = await fixture.Sut.LogoutAsync(
            fixture.HttpContext);

        var redirect = result.Should()
            .BeOfType<RedirectHttpResult>()
            .Subject;

        redirect.Url.Should().Be("/after-logout");
    }

    [Fact]
    public async Task LogoutAsync_WhenRedirectIsDisabled_ReturnsSuccessfulResponse()
    {
        var fixture = CreateFixture();

        SetupNoRedirect(fixture);

        var result = await fixture.Sut.LogoutAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<LogoutResponse>>()
            .Subject;

        ok.Value.Should().NotBeNull();
        ok.Value!.Success.Should().BeTrue();
    }

    // =====================================================================
    // LogoutDeviceSelfAsync
    // =====================================================================

    [Fact]
    public async Task LogoutDeviceSelfAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var fixture = CreateFixture(
            CreateUnauthenticatedFlow());

        var result = await fixture.Sut.LogoutDeviceSelfAsync(
            fixture.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        fixture.AccessFactory.VerifyNoOtherCalls();
        fixture.Sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogoutDeviceSelfAsync_UsesSelfActionAndForwardsChain()
    {
        var fixture = CreateFixture();

        var userKey = fixture.Flow.UserKey!.Value;
        var chainId = SessionChainId.New();

        SetJsonBody(
            fixture.HttpContext,
            new LogoutDeviceRequest
            {
                ChainId = chainId
            });

        var access = TestAccessContext.ForUser(
            userKey,
            UAuthActions.Flows.LogoutDeviceSelf);

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                fixture.Flow,
                UAuthActions.Flows.LogoutDeviceSelf,
                "flows",
                userKey.Value,
                It.IsAny<IDictionary<string, object>?>()))
            .ReturnsAsync(access);

        var expected = new RevokeResult
        {
            CurrentChain = false,
            RootRevoked = false
        };

        fixture.Sessions
            .Setup(x => x.LogoutDeviceAsync(
                access,
                chainId,
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(expected);

        var result = await fixture.Sut.LogoutDeviceSelfAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<RevokeResult>>()
            .Subject;

        ok.Value.Should().BeSameAs(expected);

        fixture.AccessFactory.Verify(x => x.CreateAsync(
            fixture.Flow,
            UAuthActions.Flows.LogoutDeviceSelf,
            "flows",
            userKey.Value,
            It.IsAny<IDictionary<string, object>?>()),
            Times.Once);

        fixture.Sessions.Verify(x => x.LogoutDeviceAsync(
            access,
            chainId,
            fixture.HttpContext.RequestAborted),
            Times.Once);
    }

    // =====================================================================
    // LogoutDeviceAdminAsync
    // =====================================================================

    [Fact]
    public async Task LogoutDeviceAdminAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var fixture = CreateFixture(
            CreateUnauthenticatedFlow());

        var targetUser = UserKey.New();

        var result = await fixture.Sut.LogoutDeviceAdminAsync(
            fixture.HttpContext,
            targetUser);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        fixture.AccessFactory.VerifyNoOtherCalls();
        fixture.Sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogoutDeviceAdminAsync_UsesAdminActionAndTargetUser()
    {
        var fixture = CreateFixture();

        var targetUser = UserKey.New();
        var chainId = SessionChainId.New();

        SetJsonBody(
            fixture.HttpContext,
            new LogoutDeviceRequest
            {
                ChainId = chainId
            });

        var access = TestAccessContext.ForUser(
            fixture.Flow.UserKey!.Value,
            UAuthActions.Flows.LogoutDeviceAdmin);

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                fixture.Flow,
                UAuthActions.Flows.LogoutDeviceAdmin,
                "flows",
                targetUser.Value,
                It.IsAny<IDictionary<string, object>?>()))
            .ReturnsAsync(access);

        var expected = new RevokeResult
        {
            CurrentChain = false,
            RootRevoked = false
        };

        fixture.Sessions
            .Setup(x => x.LogoutDeviceAsync(
                access,
                chainId,
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(expected);

        var result = await fixture.Sut.LogoutDeviceAdminAsync(
            fixture.HttpContext,
            targetUser);

        var ok = result.Should()
            .BeOfType<Ok<RevokeResult>>()
            .Subject;

        ok.Value.Should().BeSameAs(expected);

        fixture.AccessFactory.Verify(x => x.CreateAsync(
            fixture.Flow,
            UAuthActions.Flows.LogoutDeviceAdmin,
            "flows",
            targetUser.Value,
            It.IsAny<IDictionary<string, object>?>()),
            Times.Once);

        fixture.Sessions.Verify(x => x.LogoutDeviceAsync(
            access,
            chainId,
            fixture.HttpContext.RequestAborted),
            Times.Once);
    }

    // =====================================================================
    // LogoutOthersSelfAsync
    // =====================================================================

    [Fact]
    public async Task LogoutOthersSelfAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var fixture = CreateFixture(
            CreateUnauthenticatedFlow());

        var result = await fixture.Sut.LogoutOthersSelfAsync(
            fixture.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        fixture.AccessFactory.VerifyNoOtherCalls();
        fixture.Sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogoutOthersSelfAsync_WhenActorChainIsMissing_ReturnsUnauthorized()
    {
        var fixture = CreateFixture();

        var userKey = fixture.Flow.UserKey!.Value;

        var access = TestAccessContext.ForUser(
            userKey,
            UAuthActions.Flows.LogoutOthersSelf);

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                fixture.Flow,
                UAuthActions.Flows.LogoutOthersSelf,
                "flows",
                userKey.Value,
                It.IsAny<IDictionary<string, object>?>()))
            .ReturnsAsync(access);

        var result = await fixture.Sut.LogoutOthersSelfAsync(
            fixture.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        fixture.Sessions.Verify(
            x => x.LogoutOtherDevicesAsync(
                It.IsAny<AccessContext>(),
                It.IsAny<UserKey>(),
                It.IsAny<SessionChainId>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogoutOthersSelfAsync_UsesActorChainAsCurrentChain()
    {
        var fixture = CreateFixture();

        var userKey = fixture.Flow.UserKey!.Value;
        var currentChainId = SessionChainId.New();

        var access = TestAccessContext.ForUser(
            userKey,
            UAuthActions.Flows.LogoutOthersSelf,
            actorChainId: currentChainId);

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                fixture.Flow,
                UAuthActions.Flows.LogoutOthersSelf,
                "flows",
                userKey.Value,
                It.IsAny<IDictionary<string, object>?>()))
            .ReturnsAsync(access);

        fixture.Sessions
            .Setup(x => x.LogoutOtherDevicesAsync(
                access,
                userKey,
                currentChainId,
                fixture.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await fixture.Sut.LogoutOthersSelfAsync(
            fixture.HttpContext);

        result.Should().BeOfType<Ok>();

        fixture.Sessions.Verify(x => x.LogoutOtherDevicesAsync(
            access,
            userKey,
            currentChainId,
            fixture.HttpContext.RequestAborted),
            Times.Once);
    }

    // =====================================================================
    // LogoutOthersAdminAsync
    // =====================================================================

    [Fact]
    public async Task LogoutOthersAdminAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var fixture = CreateFixture(
            CreateUnauthenticatedFlow());

        var targetUser = UserKey.New();

        var result = await fixture.Sut.LogoutOthersAdminAsync(
            fixture.HttpContext,
            targetUser);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        fixture.AccessFactory.VerifyNoOtherCalls();
        fixture.Sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogoutOthersAdminAsync_UsesRequestedCurrentChain()
    {
        var fixture = CreateFixture();

        var targetUser = UserKey.New();
        var currentChainId = SessionChainId.New();

        SetJsonBody(
            fixture.HttpContext,
            new LogoutOtherDevicesRequest
            {
                CurrentChainId = currentChainId
            });

        var access = TestAccessContext.ForUser(
            fixture.Flow.UserKey!.Value,
            UAuthActions.Flows.LogoutOthersAdmin);

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                fixture.Flow,
                UAuthActions.Flows.LogoutOthersAdmin,
                "flows",
                targetUser.Value,
                It.IsAny<IDictionary<string, object>?>()))
            .ReturnsAsync(access);

        fixture.Sessions
            .Setup(x => x.LogoutOtherDevicesAsync(
                access,
                targetUser,
                currentChainId,
                fixture.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await fixture.Sut.LogoutOthersAdminAsync(
            fixture.HttpContext,
            targetUser);

        result.Should().BeOfType<Ok>();

        fixture.Sessions.Verify(x => x.LogoutOtherDevicesAsync(
            access,
            targetUser,
            currentChainId,
            fixture.HttpContext.RequestAborted),
            Times.Once);
    }

    // =====================================================================
    // LogoutAllSelfAsync
    // =====================================================================

    [Fact]
    public async Task LogoutAllSelfAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var fixture = CreateFixture(
            CreateUnauthenticatedFlow());

        var result = await fixture.Sut.LogoutAllSelfAsync(
            fixture.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        fixture.AccessFactory.VerifyNoOtherCalls();
        fixture.Sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogoutAllSelfAsync_UsesSelfActionAndCurrentUser()
    {
        var fixture = CreateFixture();

        var userKey = fixture.Flow.UserKey!.Value;

        var access = TestAccessContext.ForUser(
            userKey,
            UAuthActions.Flows.LogoutAllSelf);

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                fixture.Flow,
                UAuthActions.Flows.LogoutAllSelf,
                "flows",
                userKey.Value,
                It.IsAny<IDictionary<string, object>?>()))
            .ReturnsAsync(access);

        fixture.Sessions
            .Setup(x => x.LogoutAllDevicesAsync(
                access,
                userKey,
                fixture.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await fixture.Sut.LogoutAllSelfAsync(
            fixture.HttpContext);

        result.Should().BeOfType<Ok>();

        fixture.Sessions.Verify(x => x.LogoutAllDevicesAsync(
            access,
            userKey,
            fixture.HttpContext.RequestAborted),
            Times.Once);
    }

    // =====================================================================
    // LogoutAllAdminAsync
    // =====================================================================

    [Fact]
    public async Task LogoutAllAdminAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var fixture = CreateFixture(
            CreateUnauthenticatedFlow());

        var result = await fixture.Sut.LogoutAllAdminAsync(
            fixture.HttpContext,
            UserKey.New());

        result.Should().BeOfType<UnauthorizedHttpResult>();

        fixture.AccessFactory.VerifyNoOtherCalls();
        fixture.Sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogoutAllAdminAsync_UsesAdminActionAndTargetUser()
    {
        var fixture = CreateFixture();

        var targetUser = UserKey.New();

        var access = TestAccessContext.ForUser(
            fixture.Flow.UserKey!.Value,
            UAuthActions.Flows.LogoutAllAdmin);

        fixture.AccessFactory
            .Setup(x => x.CreateAsync(
                fixture.Flow,
                UAuthActions.Flows.LogoutAllAdmin,
                "flows",
                targetUser.Value,
                It.IsAny<IDictionary<string, object>?>()))
            .ReturnsAsync(access);

        fixture.Sessions
            .Setup(x => x.LogoutAllDevicesAsync(
                access,
                targetUser,
                fixture.HttpContext.RequestAborted))
            .Returns(Task.CompletedTask);

        var result = await fixture.Sut.LogoutAllAdminAsync(
            fixture.HttpContext,
            targetUser);

        result.Should().BeOfType<Ok>();

        fixture.Sessions.Verify(x => x.LogoutAllDevicesAsync(
            access,
            targetUser,
            fixture.HttpContext.RequestAborted),
            Times.Once);
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static Fixture CreateFixture(
        AuthFlowContext? flow = null)
    {
        flow ??= AuthFlowTestFactory.LoginSuccess();

        var authContext =
            new Mock<IAuthFlowContextAccessor>(MockBehavior.Strict);

        var flowService =
            new Mock<IUAuthFlowService>(MockBehavior.Strict);

        var accessFactory =
            new Mock<IAccessContextFactory>(MockBehavior.Strict);

        var sessions =
            new Mock<ISessionApplicationService>(MockBehavior.Strict);

        var clock =
            new Mock<IClock>(MockBehavior.Strict);

        var cookieManager =
            new Mock<IUAuthCookieManager>(MockBehavior.Strict);

        var redirectResolver =
            new Mock<IAuthRedirectResolver>(MockBehavior.Strict);

        authContext
            .SetupGet(x => x.Current)
            .Returns(flow);

        var httpContext = new DefaultHttpContext();

        var sut = new LogoutEndpointHandler(
            authContext.Object,
            flowService.Object,
            accessFactory.Object,
            sessions.Object,
            cookieManager.Object,
            redirectResolver.Object);

        return new Fixture(
            sut,
            flow,
            flowService,
            accessFactory,
            sessions,
            cookieManager,
            redirectResolver,
            httpContext);
    }

    private static AuthFlowContext CreateFlowWithSession(
        SessionSecurityContext session)
    {
        var source = AuthFlowTestFactory.LoginSuccess();

        return new AuthFlowContext(
            flowType: source.FlowType,
            clientProfile: source.ClientProfile,
            effectiveMode: source.EffectiveMode,
            device: source.Device,
            tenantKey: source.Tenant,
            isAuthenticated: true,
            userKey: session.UserKey,
            session: session,
            originalOptions: source.OriginalOptions,
            effectiveOptions: source.EffectiveOptions,
            response: source.Response,
            primaryTokenKind: source.PrimaryTokenKind,
            returnUrlInfo: source.ReturnUrlInfo
        );
    }

    private static AuthFlowContext CreateUnauthenticatedFlow()
    {
        var source = AuthFlowTestFactory.LoginSuccess();

        return new AuthFlowContext(
            flowType: source.FlowType,
            clientProfile: source.ClientProfile,
            effectiveMode: source.EffectiveMode,
            device: source.Device,
            tenantKey: source.Tenant,
            isAuthenticated: false,
            userKey: null,
            session: null,
            originalOptions: source.OriginalOptions,
            effectiveOptions: source.EffectiveOptions,
            response: source.Response,
            primaryTokenKind: source.PrimaryTokenKind,
            returnUrlInfo: source.ReturnUrlInfo
        );
    }

    private static void SetupNoRedirect(Fixture fixture)
    {
        fixture.RedirectResolver
            .Setup(x => x.ResolveSuccess(
                fixture.Flow,
                fixture.HttpContext))
            .Returns(new RedirectDecision(
                enabled: false,
                targetUrl: null));
    }

    private static void SetJsonBody(
        HttpContext context,
        object value)
    {
        var json = JsonSerializer.Serialize(value);
        var bytes = Encoding.UTF8.GetBytes(json);

        context.Request.ContentType = "application/json";
        context.Request.ContentLength = bytes.Length;
        context.Request.Body = new MemoryStream(bytes);
    }

    private sealed record Fixture(
        LogoutEndpointHandler Sut,
        AuthFlowContext Flow,
        Mock<IUAuthFlowService> FlowService,
        Mock<IAccessContextFactory> AccessFactory,
        Mock<ISessionApplicationService> Sessions,
        Mock<IUAuthCookieManager> CookieManager,
        Mock<IAuthRedirectResolver> RedirectResolver,
        DefaultHttpContext HttpContext);
}