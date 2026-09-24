using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class RefreshFlowServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly AuthSessionId SessionId = TestIds.Session("refresh-flow-session");
    private static readonly DeviceContext Device = TestDevice.Default();

    [Fact]
    public async Task RefreshAsync_PureOpaque_WhenSessionIdMissing_ReturnsReauthWithoutCallingDependencies()
    {
        var (sut, validator, touch, rotation) = CreateSut();

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.PureOpaque),
            CreateRequest(sessionId: null));

        AssertReauth(result);
        validator.VerifyNoOtherCalls();
        touch.VerifyNoOtherCalls();
        rotation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_PureOpaque_WhenSessionInvalid_ReturnsReauthWithoutTouching()
    {
        var (sut, validator, touch, rotation) = CreateSut();

        validator
            .Setup(x => x.ValidateSessionAsync(
                It.Is<SessionValidationContext>(c =>
                    c.Tenant == TenantKey.Single &&
                    c.SessionId == SessionId &&
                    c.Now == Now &&
                    c.Device == Device),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SessionValidationResult.Invalid(SessionState.Revoked));

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.PureOpaque),
            CreateRequest(SessionId));

        AssertReauth(result);
        touch.VerifyNoOtherCalls();
        rotation.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false, RefreshOutcome.NoOp)]
    [InlineData(true, RefreshOutcome.Touched)]
    public async Task RefreshAsync_PureOpaque_WhenSessionRefreshSucceeds_ReturnsExpectedOutcome(
        bool didTouch,
        RefreshOutcome expectedOutcome)
    {
        var (sut, validator, touch, rotation) = CreateSut();
        var validation = CreateActiveValidation();

        validator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validation);

        touch
            .Setup(x => x.RefreshAsync(
                validation,
                It.Is<SessionTouchPolicy>(p => p.TouchInterval == TimeSpan.FromMinutes(5)),
                SessionTouchMode.IfNeeded,
                Now,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SessionRefreshResult.Success(SessionId, didTouch));

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.PureOpaque),
            CreateRequest(SessionId));

        result.Succeeded.Should().BeTrue();
        result.Outcome.Should().Be(expectedOutcome);
        result.SessionId.Should().Be(SessionId);
        rotation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_PureOpaque_WhenTouchFails_ReturnsReauth()
    {
        var (sut, validator, touch, rotation) = CreateSut();
        var validation = CreateActiveValidation();

        validator
            .Setup(x => x.ValidateSessionAsync(It.IsAny<SessionValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validation);

        touch
            .Setup(x => x.RefreshAsync(
                validation,
                It.IsAny<SessionTouchPolicy>(),
                It.IsAny<SessionTouchMode>(),
                Now,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SessionRefreshResult.Failed());

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.PureOpaque),
            CreateRequest(SessionId));

        AssertReauth(result);
        rotation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_PureJwt_WhenRefreshTokenMissing_ReturnsReauthWithoutCallingDependencies()
    {
        var (sut, validator, touch, rotation) = CreateSut();

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.PureJwt),
            CreateRequest(refreshToken: "   "));

        AssertReauth(result);
        validator.VerifyNoOtherCalls();
        touch.VerifyNoOtherCalls();
        rotation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_PureJwt_WhenRotationSucceeds_ReturnsRotatedTokensWithoutSessionValidation()
    {
        var (sut, validator, touch, rotation) = CreateSut();
        var execution = CreateSuccessfulRotation();

        rotation
            .Setup(x => x.RotateAsync(
                It.IsAny<AuthFlowContext>(),
                It.Is<RefreshTokenRotationContext>(c =>
                    c.RefreshToken == "refresh-token" &&
                    c.Now == Now &&
                    c.Device == Device &&
                    c.ExpectedSessionId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.PureJwt),
            CreateRequest(refreshToken: "refresh-token"));

        result.Succeeded.Should().BeTrue();
        result.Outcome.Should().Be(RefreshOutcome.Rotated);
        result.SessionId.Should().BeNull();
        result.AccessToken.Should().BeSameAs(execution.Result.AccessToken);
        result.RefreshToken.Should().BeSameAs(execution.Result.RefreshToken);
        validator.VerifyNoOtherCalls();
        touch.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_PureJwt_WhenRotationFails_ReturnsReauth()
    {
        var (sut, validator, touch, rotation) = CreateSut();

        rotation
            .Setup(x => x.RotateAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<RefreshTokenRotationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenRotationExecution
            {
                Result = RefreshTokenRotationResult.Failed()
            });

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.PureJwt),
            CreateRequest(refreshToken: "refresh-token"));

        AssertReauth(result);
        validator.VerifyNoOtherCalls();
        touch.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, "refresh-token")]
    [InlineData("session", null)]
    [InlineData("session", "   ")]
    public async Task RefreshAsync_Hybrid_WhenRequiredCredentialMissing_ReturnsReauthWithoutCallingDependencies(
        string? sessionMarker,
        string? refreshToken)
    {
        var (sut, validator, touch, rotation) = CreateSut();

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.Hybrid),
            CreateRequest(sessionMarker is null ? null : SessionId, refreshToken));

        AssertReauth(result);
        validator.VerifyNoOtherCalls();
        touch.VerifyNoOtherCalls();
        rotation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_Hybrid_WhenSessionInvalid_ReturnsReauthWithoutRotationOrTouch()
    {
        var (sut, validator, touch, rotation) = CreateSut();

        validator
            .Setup(x => x.ValidateSessionAsync(It.IsAny<SessionValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SessionValidationResult.Invalid(SessionState.DeviceMismatch));

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.Hybrid),
            CreateRequest(SessionId, "refresh-token"));

        AssertReauth(result);
        rotation.VerifyNoOtherCalls();
        touch.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_Hybrid_WhenRotationFails_ReturnsReauthWithoutTouch()
    {
        var (sut, validator, touch, rotation) = CreateSut();
        var validation = CreateActiveValidation();

        validator
            .Setup(x => x.ValidateSessionAsync(It.IsAny<SessionValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validation);

        rotation
            .Setup(x => x.RotateAsync(
                It.IsAny<AuthFlowContext>(),
                It.Is<RefreshTokenRotationContext>(c =>
                    c.ExpectedSessionId == SessionId &&
                    c.RefreshToken == "refresh-token" &&
                    c.Now == Now),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenRotationExecution
            {
                Result = RefreshTokenRotationResult.Failed()
            });

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.Hybrid),
            CreateRequest(SessionId, "refresh-token"));

        AssertReauth(result);
        touch.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_Hybrid_WhenAllStepsSucceed_ReturnsRotatedResultAndTouchesSession()
    {
        var (sut, validator, touch, rotation) = CreateSut();
        var validation = CreateActiveValidation();
        var execution = CreateSuccessfulRotation();

        validator
            .Setup(x => x.ValidateSessionAsync(It.IsAny<SessionValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validation);

        rotation
            .Setup(x => x.RotateAsync(
                It.IsAny<AuthFlowContext>(),
                It.Is<RefreshTokenRotationContext>(c => c.ExpectedSessionId == SessionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);

        touch
            .Setup(x => x.RefreshAsync(
                validation,
                It.Is<SessionTouchPolicy>(p => p.TouchInterval == TimeSpan.FromMinutes(5)),
                SessionTouchMode.IfNeeded,
                Now,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SessionRefreshResult.Success(SessionId, didTouch: true));

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.Hybrid),
            CreateRequest(SessionId, "refresh-token"));

        result.Succeeded.Should().BeTrue();
        result.Outcome.Should().Be(RefreshOutcome.Rotated);
        result.SessionId.Should().Be(SessionId);
        result.AccessToken.Should().BeSameAs(execution.Result.AccessToken);
        result.RefreshToken.Should().BeSameAs(execution.Result.RefreshToken);
    }

    [Fact]
    public async Task RefreshAsync_Hybrid_WhenTouchFailsAfterRotation_ReturnsReauth()
    {
        var (sut, validator, touch, rotation) = CreateSut();
        var validation = CreateActiveValidation();

        validator
            .Setup(x => x.ValidateSessionAsync(It.IsAny<SessionValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validation);

        rotation
            .Setup(x => x.RotateAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<RefreshTokenRotationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessfulRotation());

        touch
            .Setup(x => x.RefreshAsync(
                validation,
                It.IsAny<SessionTouchPolicy>(),
                It.IsAny<SessionTouchMode>(),
                Now,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SessionRefreshResult.Failed());

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.Hybrid),
            CreateRequest(SessionId, "refresh-token"));

        AssertReauth(result);
        rotation.Verify(x => x.RotateAsync(
            It.IsAny<AuthFlowContext>(),
            It.IsAny<RefreshTokenRotationContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_SemiHybrid_WhenSessionInvalid_ReturnsReauthWithoutRotation()
    {
        var (sut, validator, touch, rotation) = CreateSut();

        validator
            .Setup(x => x.ValidateSessionAsync(It.IsAny<SessionValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SessionValidationResult.Invalid(SessionState.SecurityMismatch));

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.SemiHybrid),
            CreateRequest(SessionId, "refresh-token"));

        AssertReauth(result);
        rotation.VerifyNoOtherCalls();
        touch.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_SemiHybrid_WhenRotationSucceeds_ReturnsRotatedWithoutTouchingSession()
    {
        var (sut, validator, touch, rotation) = CreateSut();
        var validation = CreateActiveValidation();
        var execution = CreateSuccessfulRotation();

        validator
            .Setup(x => x.ValidateSessionAsync(It.IsAny<SessionValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validation);

        rotation
            .Setup(x => x.RotateAsync(
                It.IsAny<AuthFlowContext>(),
                It.Is<RefreshTokenRotationContext>(c =>
                    c.ExpectedSessionId == SessionId &&
                    c.RefreshToken == "refresh-token"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.SemiHybrid),
            CreateRequest(SessionId, "refresh-token"));

        result.Succeeded.Should().BeTrue();
        result.Outcome.Should().Be(RefreshOutcome.Rotated);
        result.SessionId.Should().Be(SessionId);
        result.AccessToken.Should().BeSameAs(execution.Result.AccessToken);
        result.RefreshToken.Should().BeSameAs(execution.Result.RefreshToken);
        touch.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefreshAsync_SemiHybrid_WhenRotationFails_ReturnsReauthWithoutTouch()
    {
        var (sut, validator, touch, rotation) = CreateSut();
        var validation = CreateActiveValidation();

        validator
            .Setup(x => x.ValidateSessionAsync(It.IsAny<SessionValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validation);

        rotation
            .Setup(x => x.RotateAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<RefreshTokenRotationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenRotationExecution
            {
                Result = RefreshTokenRotationResult.Failed()
            });

        var result = await sut.RefreshAsync(
            CreateFlow(UAuthMode.SemiHybrid),
            CreateRequest(SessionId, "refresh-token"));

        AssertReauth(result);
        touch.VerifyNoOtherCalls();
    }

    private static (
        RefreshFlowService Sut,
        Mock<ISessionValidator> Validator,
        Mock<ISessionTouchService> Touch,
        Mock<IRefreshTokenRotationService> Rotation) CreateSut()
    {
        var validator = new Mock<ISessionValidator>(MockBehavior.Strict);
        var touch = new Mock<ISessionTouchService>(MockBehavior.Strict);
        var rotation = new Mock<IRefreshTokenRotationService>(MockBehavior.Strict);

        var sut = new RefreshFlowService(
            validator.Object,
            touch.Object,
            rotation.Object,
            new TestClock(Now));

        return (sut, validator, touch, rotation);
    }

    private static RefreshFlowRequest CreateRequest(
        AuthSessionId? sessionId = null,
        string? refreshToken = null)
        => new()
        {
            SessionId = sessionId,
            RefreshToken = refreshToken,
            Device = Device,
            TouchMode = SessionTouchMode.IfNeeded
        };

    private static SessionValidationResult CreateActiveValidation()
        => SessionValidationResult.Active(
            TenantKey.Single,
            UserKey.New(),
            SessionId,
            SessionChainId.New(),
            SessionRootId.New(),
            ClaimsSnapshot.Empty,
            Now.AddHours(-1),
            Device.DeviceId);

    private static RefreshTokenRotationExecution CreateSuccessfulRotation()
    {
        var access = new AccessToken
        {
            Token = "new-access-token",
            Format = TokenFormat.Jwt,
            ExpiresAt = Now.AddMinutes(15)
        };

        var refresh = new RefreshTokenInfo
        {
            Token = "new-refresh-token",
            TokenHash = "new-refresh-token-hash",
            ExpiresAt = Now.AddDays(7)
        };

        return new RefreshTokenRotationExecution
        {
            Tenant = TenantKey.Single,
            UserKey = UserKey.New(),
            SessionId = SessionId,
            ChainId = SessionChainId.New(),
            Result = RefreshTokenRotationResult.Success(access, refresh)
        };
    }

    private static AuthFlowContext CreateFlow(UAuthMode mode)
    {
        var original = TestServerOptions.Default();
        var effective = TestServerOptions.Effective(mode);
        effective.Options.Session.TouchInterval = TimeSpan.FromMinutes(5);

        return new AuthFlowContext(
            flowType: AuthFlowType.RefreshSession,
            clientProfile: UAuthClientProfile.Api,
            effectiveMode: mode,
            device: Device,
            tenantKey: TenantKey.Single,
            isAuthenticated: true,
            userKey: UserKey.New(),
            session: null,
            originalOptions: original,
            effectiveOptions: effective,
            response: new EffectiveAuthResponse(
                sessionIdDelivery: CredentialResponseOptions.Disabled(GrantKind.Session),
                accessTokenDelivery: CredentialResponseOptions.Disabled(GrantKind.AccessToken),
                refreshTokenDelivery: CredentialResponseOptions.Disabled(GrantKind.RefreshToken),
                redirect: EffectiveRedirectResponse.Disabled),
            primaryTokenKind: PrimaryTokenKind.AccessToken,
            returnUrlInfo: ReturnUrlInfo.None());
    }

    private static void AssertReauth(RefreshFlowResult result)
    {
        result.Succeeded.Should().BeFalse();
        result.Outcome.Should().Be(RefreshOutcome.ReauthRequired);
        result.SessionId.Should().BeNull();
        result.AccessToken.Should().BeNull();
        result.RefreshToken.Should().BeNull();
    }
}
