using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Events;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class UAuthFlowServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------------------
    // LoginAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task LoginAsync_DelegatesToLoginOrchestrator()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();

        var request = new LoginRequest
        {
            Identifier = "user@example.com",
            Secret = "password"
        };

        var expected = LoginResult.SuccessPreview();

        var login = new Mock<ILoginOrchestrator>(MockBehavior.Strict);

        login
            .Setup(x => x.LoginAsync(
                flow,
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = CreateSut(
            loginOrchestrator: login.Object);

        var result = await sut.LoginAsync(
            flow,
            request);

        result.Should().BeSameAs(expected);

        login.Verify(
            x => x.LoginAsync(
                flow,
                request,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithClientProfileOverride_RecreatesFlowBeforeLogin()
    {
        var originalFlow = AuthFlowTestFactory.LoginSuccess();
        var recreatedFlow = AuthFlowTestFactory.LoginSuccess();

        var request = new LoginRequest
        {
            Identifier = "user@example.com",
            Secret = "password"
        };

        var execution = new AuthExecutionContext
        {
            EffectiveClientProfile = UAuthClientProfile.Api,
            Device = null
        };

        var expected = LoginResult.SuccessPreview();

        var factory = new Mock<IAuthFlowContextFactory>(MockBehavior.Strict);
        var login = new Mock<ILoginOrchestrator>(MockBehavior.Strict);

        factory
            .Setup(x => x.RecreateWithClientProfileAsync(
                originalFlow,
                UAuthClientProfile.Api,
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<AuthFlowContext>(recreatedFlow));

        login
            .Setup(x => x.LoginAsync(
                recreatedFlow,
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = CreateSut(
            authFlowContextFactory: factory.Object,
            loginOrchestrator: login.Object);

        var result = await sut.LoginAsync(
            originalFlow,
            execution,
            request);

        result.Should().BeSameAs(expected);

        factory.Verify(
            x => x.RecreateWithClientProfileAsync(
                originalFlow,
                UAuthClientProfile.Api,
                It.IsAny<CancellationToken>()),
            Times.Once);

        factory.Verify(
            x => x.RecreateWithDeviceAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<DeviceContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithDeviceOverride_RecreatesFlowBeforeLogin()
    {
        var originalFlow = AuthFlowTestFactory.LoginSuccess();
        var recreatedFlow = AuthFlowTestFactory.LoginSuccess();
        var device = TestDevice.Default();

        var request = new LoginRequest
        {
            Identifier = "user@example.com",
            Secret = "password"
        };

        var execution = new AuthExecutionContext
        {
            EffectiveClientProfile = null,
            Device = device
        };

        var expected = LoginResult.SuccessPreview();

        var factory = new Mock<IAuthFlowContextFactory>(MockBehavior.Strict);
        var login = new Mock<ILoginOrchestrator>(MockBehavior.Strict);

        factory
            .Setup(x => x.RecreateWithDeviceAsync(
                originalFlow,
                device,
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<AuthFlowContext>(recreatedFlow));

        login
            .Setup(x => x.LoginAsync(
                recreatedFlow,
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = CreateSut(
            authFlowContextFactory: factory.Object,
            loginOrchestrator: login.Object);

        var result = await sut.LoginAsync(
            originalFlow,
            execution,
            request);

        result.Should().BeSameAs(expected);

        factory.Verify(
            x => x.RecreateWithClientProfileAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<UAuthClientProfile>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithClientProfileAndDeviceOverride_AppliesOverridesInOrder()
    {
        var originalFlow = AuthFlowTestFactory.LoginSuccess();
        var profileFlow = AuthFlowTestFactory.LoginSuccess();
        var finalFlow = AuthFlowTestFactory.LoginSuccess();

        var device = TestDevice.Default();

        var request = new LoginRequest
        {
            Identifier = "user@example.com",
            Secret = "password"
        };

        var execution = new AuthExecutionContext
        {
            EffectiveClientProfile = UAuthClientProfile.Api,
            Device = device
        };

        var expected = LoginResult.SuccessPreview();

        var factory = new Mock<IAuthFlowContextFactory>(MockBehavior.Strict);
        var login = new Mock<ILoginOrchestrator>(MockBehavior.Strict);

        factory
            .Setup(x => x.RecreateWithClientProfileAsync(
                originalFlow,
                UAuthClientProfile.Api,
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<AuthFlowContext>(profileFlow));

        factory
            .Setup(x => x.RecreateWithDeviceAsync(
                profileFlow,
                device,
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<AuthFlowContext>(finalFlow));

        login
            .Setup(x => x.LoginAsync(
                finalFlow,
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = CreateSut(
            authFlowContextFactory: factory.Object,
            loginOrchestrator: login.Object);

        var result = await sut.LoginAsync(
            originalFlow,
            execution,
            request);

        result.Should().BeSameAs(expected);

        factory.Verify(
            x => x.RecreateWithDeviceAsync(
                profileFlow,
                device,
                It.IsAny<CancellationToken>()),
            Times.Once);

        login.Verify(
            x => x.LoginAsync(
                finalFlow,
                request,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---------------------------------------------------------------------
    // Internal Login
    // ---------------------------------------------------------------------

    [Fact]
    public async Task InternalLoginAsync_DelegatesToInternalLoginOrchestrator()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();

        var request = new LoginRequest
        {
            Identifier = "user@example.com",
            Secret = "password"
        };

        var options = new LoginExecutionOptions();

        var expected = LoginResult.SuccessPreview();

        var internalLogin =
            new Mock<IInternalLoginOrchestrator>(MockBehavior.Strict);

        internalLogin
            .Setup(x => x.LoginAsync(
                flow,
                request,
                options,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = CreateSut(
            internalLoginOrchestrator: internalLogin.Object);

        var result = await sut.LoginAsync(
            flow,
            request,
            options);

        result.Should().BeSameAs(expected);

        internalLogin.Verify(
            x => x.LoginAsync(
                flow,
                request,
                options,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InternalLoginAsync_WithExecutionOverrides_UsesRecreatedFlow()
    {
        var originalFlow = AuthFlowTestFactory.LoginSuccess();
        var profileFlow = AuthFlowTestFactory.LoginSuccess();
        var finalFlow = AuthFlowTestFactory.LoginSuccess();

        var device = TestDevice.Default();

        var execution = new AuthExecutionContext
        {
            EffectiveClientProfile = UAuthClientProfile.Api,
            Device = device
        };

        var request = new LoginRequest
        {
            Identifier = "user@example.com",
            Secret = "password"
        };

        var options = new LoginExecutionOptions();
        var expected = LoginResult.SuccessPreview();

        var factory = new Mock<IAuthFlowContextFactory>(MockBehavior.Strict);

        var internalLogin =
            new Mock<IInternalLoginOrchestrator>(MockBehavior.Strict);

        factory
            .Setup(x => x.RecreateWithClientProfileAsync(
                originalFlow,
                UAuthClientProfile.Api,
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<AuthFlowContext>(profileFlow));

        factory
            .Setup(x => x.RecreateWithDeviceAsync(
                profileFlow,
                device,
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<AuthFlowContext>(finalFlow));

        internalLogin
            .Setup(x => x.LoginAsync(
                finalFlow,
                request,
                options,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = CreateSut(
            authFlowContextFactory: factory.Object,
            internalLoginOrchestrator: internalLogin.Object);

        var result = await sut.LoginAsync(
            originalFlow,
            execution,
            request,
            options);

        result.Should().BeSameAs(expected);
    }

    // ---------------------------------------------------------------------
    // LogoutAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task LogoutAsync_WhenSessionCannotBeResolved_DoesNotDispatchLogoutEvent()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();
        var sessionId = TestIds.Session("logout-session");

        var accessor = CreateAccessor(flow);

        var orchestrator =
            new Mock<ISessionOrchestrator>(MockBehavior.Strict);

        orchestrator
            .Setup(x => x.ExecuteAsync(
                It.IsAny<AuthContext>(),
                It.IsAny<ISessionCommand<bool>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var eventRaised = false;

        var events = new UAuthEvents
        {
            OnUserLoggedOut = _ =>
            {
                eventRaised = true;
                return Task.CompletedTask;
            }
        };

        var sut = CreateSut(
            authFlow: accessor.Object,
            orchestrator: orchestrator.Object,
            events: new UAuthEventDispatcher(events));

        await sut.LogoutAsync(
            new LogoutRequest
            {
                SessionId = sessionId
            });

        eventRaised.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAsync_WhenLogoutSucceeds_DispatchesUserLoggedOutEvent()
    {
        var flow = AuthFlowTestFactory.LoginSuccess();
        var sessionId = TestIds.Session("logout-session");

        var accessor = CreateAccessor(flow);

        var orchestrator =
            new Mock<ISessionOrchestrator>(MockBehavior.Strict);

        orchestrator
            .Setup(x => x.ExecuteAsync(
                It.IsAny<AuthContext>(),
                It.IsAny<ISessionCommand<bool>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        UserLoggedOutContext? captured = null;

        var events = new UAuthEvents
        {
            OnUserLoggedOut = context =>
            {
                captured = context;
                return Task.CompletedTask;
            }
        };

        var sut = CreateSut(
            authFlow: accessor.Object,
            orchestrator: orchestrator.Object,
            events: new UAuthEventDispatcher(events));

        await sut.LogoutAsync(
            new LogoutRequest
            {
                SessionId = sessionId
            });

        captured.Should().NotBeNull();

        captured!.Tenant.Should().Be(flow.Tenant);
        captured.UserKey.Should().Be(flow.UserKey!.Value);
        captured.LoggedOutAt.Should().Be(Now);
        captured.Reason.Should().Be(LogoutReason.Explicit);
        captured.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public async Task LogoutAsync_WhenLogoutSucceedsButUserKeyIsMissing_DoesNotDispatchEvent()
    {
        var flow = CreateFlow(
            userKey: null,
            session: null);

        var sessionId = TestIds.Session("logout-session");

        var accessor = CreateAccessor(flow);

        var orchestrator =
            new Mock<ISessionOrchestrator>(MockBehavior.Strict);

        orchestrator
            .Setup(x => x.ExecuteAsync(
                It.IsAny<AuthContext>(),
                It.IsAny<ISessionCommand<bool>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var eventRaised = false;

        var events = new UAuthEvents
        {
            OnUserLoggedOut = _ =>
            {
                eventRaised = true;
                return Task.CompletedTask;
            }
        };

        var sut = CreateSut(
            authFlow: accessor.Object,
            orchestrator: orchestrator.Object,
            events: new UAuthEventDispatcher(events));

        await sut.LogoutAsync(
            new LogoutRequest
            {
                SessionId = sessionId
            });

        eventRaised.Should().BeFalse();
    }

    // ---------------------------------------------------------------------
    // LogoutAllAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task LogoutAllAsync_WhenActiveSessionIsMissing_Throws()
    {
        var flow = CreateFlow(
            userKey: UserKey.New(),
            session: null);

        var sut = CreateSut(
            authFlow: CreateAccessor(flow).Object);

        var act = () => sut.LogoutAllAsync(
            new LogoutAllRequest
            {
                ExceptCurrent = false
            });

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*active session*");
    }

    [Fact]
    public async Task LogoutAllAsync_WhenExceptCurrentIsFalse_RevokesAllChains()
    {
        var user = UserKey.New();
        var chainId = SessionChainId.New();

        var session = new SessionSecurityContext
        {
            UserKey = user,
            SessionId = TestIds.Session("current-session"),
            ChainId = chainId,
            State = SessionState.Active
        };

        var flow = CreateFlow(
            user,
            session);

        var accessor = CreateAccessor(flow);

        RevokeAllChainsCommand? captured = null;

        var orchestrator =
            new Mock<ISessionOrchestrator>(MockBehavior.Strict);

        orchestrator
            .Setup(x => x.ExecuteAsync(
                It.IsAny<AuthContext>(),
                It.IsAny<ISessionCommand<CodeBeam.UltimateAuth.Core.Contracts.Unit>>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthContext, ISessionCommand<CodeBeam.UltimateAuth.Core.Contracts.Unit>, CancellationToken>(
                (_, command, _) =>
                {
                    captured = command.Should()
                        .BeOfType<RevokeAllChainsCommand>()
                        .Subject;
                })
            .ReturnsAsync(CodeBeam.UltimateAuth.Core.Contracts.Unit.Value);

        var sut = CreateSut(
            authFlow: accessor.Object,
            orchestrator: orchestrator.Object);

        await sut.LogoutAllAsync(
            new LogoutAllRequest
            {
                ExceptCurrent = false
            });

        captured.Should().NotBeNull();
        captured!.UserKey.Should().Be(user);
        captured.ExceptChainId.Should().BeNull();
    }

    [Fact]
    public async Task LogoutAllAsync_WhenExceptCurrentIsTrue_ExcludesCurrentChain()
    {
        var user = UserKey.New();
        var chainId = SessionChainId.New();

        var session = new SessionSecurityContext
        {
            UserKey = user,
            SessionId = TestIds.Session("current-session"),
            ChainId = chainId,
            State = SessionState.Active
        };

        var flow = CreateFlow(
            user,
            session);

        var accessor = CreateAccessor(flow);

        RevokeAllChainsCommand? captured = null;

        var orchestrator =
            new Mock<ISessionOrchestrator>(MockBehavior.Strict);

        orchestrator
            .Setup(x => x.ExecuteAsync(
                It.IsAny<AuthContext>(),
                It.IsAny<ISessionCommand<CodeBeam.UltimateAuth.Core.Contracts.Unit>>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthContext, ISessionCommand<CodeBeam.UltimateAuth.Core.Contracts.Unit>, CancellationToken>(
                (_, command, _) =>
                {
                    captured = command.Should()
                        .BeOfType<RevokeAllChainsCommand>()
                        .Subject;
                })
            .ReturnsAsync(CodeBeam.UltimateAuth.Core.Contracts.Unit.Value);

        var sut = CreateSut(
            authFlow: accessor.Object,
            orchestrator: orchestrator.Object);

        await sut.LogoutAllAsync(
            new LogoutAllRequest
            {
                ExceptCurrent = true
            });

        captured.Should().NotBeNull();
        captured!.UserKey.Should().Be(user);
        captured.ExceptChainId.Should().Be(chainId);
    }

    [Fact]
    public async Task LogoutAllAsync_WhenExceptCurrentIsTrueButChainIdIsMissing_Throws()
    {
        var user = UserKey.New();

        var session = new SessionSecurityContext
        {
            UserKey = user,
            SessionId = TestIds.Session("current-session"),
            ChainId = null,
            State = SessionState.Active
        };

        var flow = CreateFlow(
            user,
            session);

        var orchestrator =
            new Mock<ISessionOrchestrator>(MockBehavior.Strict);

        var sut = CreateSut(
            authFlow: CreateAccessor(flow).Object,
            orchestrator: orchestrator.Object);

        var act = () => sut.LogoutAllAsync(
            new LogoutAllRequest
            {
                ExceptCurrent = true
            });

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*chain could not be resolved*");

        orchestrator.Verify(
            x => x.ExecuteAsync(
                It.IsAny<AuthContext>(),
                It.IsAny<ISessionCommand<CodeBeam.UltimateAuth.Core.Contracts.Unit>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static UAuthFlowService CreateSut(
        IAuthFlowContextAccessor? authFlow = null,
        IAuthFlowContextFactory? authFlowContextFactory = null,
        ILoginOrchestrator? loginOrchestrator = null,
        IInternalLoginOrchestrator? internalLoginOrchestrator = null,
        ISessionOrchestrator? orchestrator = null,
        UAuthEventDispatcher? events = null)
    {
        return new UAuthFlowService(
            authFlow ??
                Mock.Of<IAuthFlowContextAccessor>(),

            authFlowContextFactory ??
                Mock.Of<IAuthFlowContextFactory>(),

            loginOrchestrator ??
                Mock.Of<ILoginOrchestrator>(),

            internalLoginOrchestrator ??
                Mock.Of<IInternalLoginOrchestrator>(),

            orchestrator ??
                Mock.Of<ISessionOrchestrator>(),

            events ??
                new UAuthEventDispatcher(new UAuthEvents()),

            new TestClock(Now));
    }

    private static Mock<IAuthFlowContextAccessor> CreateAccessor(
        AuthFlowContext flow)
    {
        var accessor =
            new Mock<IAuthFlowContextAccessor>(MockBehavior.Strict);

        accessor
            .SetupGet(x => x.Current)
            .Returns(flow);

        return accessor;
    }

    private static AuthFlowContext CreateFlow(
        UserKey? userKey,
        SessionSecurityContext? session)
    {
        return new AuthFlowContext(
            flowType: AuthFlowType.Logout,
            clientProfile: UAuthClientProfile.BlazorServer,
            effectiveMode: UAuthMode.PureOpaque,
            device: TestDevice.Default(),
            tenantKey: TenantKey.Single,
            isAuthenticated: userKey.HasValue,
            userKey: userKey,
            session: session,
            originalOptions: TestServerOptions.Default(),
            effectiveOptions: TestServerOptions.Effective(),
            response: new EffectiveAuthResponse(
                sessionIdDelivery:
                    CredentialResponseOptions.Disabled(
                        GrantKind.Session),
                accessTokenDelivery:
                    CredentialResponseOptions.Disabled(
                        GrantKind.AccessToken),
                refreshTokenDelivery:
                    CredentialResponseOptions.Disabled(
                        GrantKind.RefreshToken),
                redirect:
                    EffectiveRedirectResponse.Disabled),
            primaryTokenKind: PrimaryTokenKind.Session,
            returnUrlInfo: ReturnUrlInfo.None());
    }
}