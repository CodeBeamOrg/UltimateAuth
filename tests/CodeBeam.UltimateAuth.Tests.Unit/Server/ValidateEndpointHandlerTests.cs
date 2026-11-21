using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class ValidateEndpointHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    // =====================================================================
    // Missing / unsupported credential
    // =====================================================================

    [Fact]
    public async Task ValidateAsync_WhenCredentialIsMissing_ReturnsUnauthorizedNotFound()
    {
        var fixture = CreateFixture();

        fixture.CredentialResolver
            .Setup(x => x.ResolveAsync(
                fixture.HttpContext,
                fixture.Flow.Response))
            .ReturnsAsync((ResolvedCredential?)null);

        var result = await fixture.Sut.ValidateAsync(
            fixture.HttpContext,
            fixture.CancellationToken);

        var json = result.Should()
            .BeOfType<JsonHttpResult<AuthValidationResult>>()
            .Subject;

        json.StatusCode.Should()
            .Be(StatusCodes.Status401Unauthorized);

        json.Value.Should().NotBeNull();
        json.Value!.State.Should()
            .Be(SessionState.NotFound);

        json.Value.IsValid.Should().BeFalse();
        json.Value.Snapshot.Should().BeNull();

        fixture.SessionValidator.VerifyNoOtherCalls();
        fixture.SnapshotFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_WhenCredentialKindIsAccessToken_ReturnsUnauthorizedUnsupported()
    {
        var fixture = CreateFixture();

        var credential = new ResolvedCredential
        {
            Kind = PrimaryTokenKind.AccessToken,
            Value = "access-token",
            Tenant = fixture.Flow.Tenant,
            Device = TestDevice.DefaultDeviceInfo()
        };

        fixture.CredentialResolver
            .Setup(x => x.ResolveAsync(
                fixture.HttpContext,
                fixture.Flow.Response))
            .ReturnsAsync(credential);

        var result = await fixture.Sut.ValidateAsync(
            fixture.HttpContext,
            fixture.CancellationToken);

        var json = result.Should()
            .BeOfType<JsonHttpResult<AuthValidationResult>>()
            .Subject;

        json.StatusCode.Should()
            .Be(StatusCodes.Status401Unauthorized);

        json.Value.Should().NotBeNull();
        json.Value!.State.Should()
            .Be(SessionState.Unsupported);

        json.Value.Snapshot.Should().BeNull();

        fixture.SessionValidator.VerifyNoOtherCalls();
        fixture.SnapshotFactory.VerifyNoOtherCalls();
    }

    // =====================================================================
    // Invalid session credential
    // =====================================================================

    [Fact]
    public async Task ValidateAsync_WhenSessionCredentialCannotBeParsed_ReturnsUnauthorizedInvalid()
    {
        var fixture = CreateFixture();

        var credential = new ResolvedCredential
        {
            Kind = PrimaryTokenKind.Session,
            Value = "invalid",
            Tenant = fixture.Flow.Tenant,
            Device = TestDevice.DefaultDeviceInfo()
        };

        fixture.CredentialResolver
            .Setup(x => x.ResolveAsync(
                fixture.HttpContext,
                fixture.Flow.Response))
            .ReturnsAsync(credential);

        var result = await fixture.Sut.ValidateAsync(
            fixture.HttpContext,
            fixture.CancellationToken);

        var json = result.Should()
            .BeOfType<JsonHttpResult<AuthValidationResult>>()
            .Subject;

        json.StatusCode.Should()
            .Be(StatusCodes.Status401Unauthorized);

        json.Value.Should().NotBeNull();
        json.Value!.State.Should()
            .Be(SessionState.Invalid);

        json.Value.IsValid.Should().BeFalse();
        json.Value.Snapshot.Should().BeNull();

        fixture.SessionValidator.VerifyNoOtherCalls();
        fixture.SnapshotFactory.VerifyNoOtherCalls();
    }

    // =====================================================================
    // Session validation context
    // =====================================================================

    [Fact]
    public async Task ValidateAsync_WhenSessionCredentialIsValid_PassesExpectedContextToValidator()
    {
        var fixture = CreateFixture();

        var sessionId =
            TestIds.Session("validate-session");

        SetupSessionCredential(
            fixture,
            sessionId);

        SessionValidationContext? capturedContext = null;
        CancellationToken capturedToken = default;

        var validation = CreateActiveValidation(
            fixture.Flow.Tenant,
            sessionId);

        fixture.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                fixture.CancellationToken))
            .Callback<SessionValidationContext, CancellationToken>(
                (context, ct) =>
                {
                    capturedContext = context;
                    capturedToken = ct;
                })
            .ReturnsAsync(validation);

        fixture.SnapshotFactory
            .Setup(x => x.CreateAsync(
                validation,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthStateSnapshot?)null);

        await fixture.Sut.ValidateAsync(
            fixture.HttpContext,
            fixture.CancellationToken);

        capturedContext.Should().NotBeNull();

        capturedContext!.SessionId.Should()
            .Be(sessionId);

        capturedContext.Tenant.Should()
            .Be(fixture.Flow.Tenant);

        capturedContext.Now.Should()
            .Be(Now);

        capturedContext.Device.Should()
            .Be(fixture.Flow.Device);

        capturedToken.Should()
            .Be(fixture.CancellationToken);
    }

    // =====================================================================
    // Missing UserKey
    // =====================================================================

    [Fact]
    public async Task ValidateAsync_WhenValidationIsInvalidWithoutUserKey_ReturnsOkInvalid()
    {
        var fixture = CreateFixture();

        var sessionId =
            TestIds.Session("missing-user-session");

        SetupSessionCredential(
            fixture,
            sessionId);

        var validation =
            SessionValidationResult.Invalid(
                SessionState.Invalid,
                userKey: null,
                sessionId: sessionId);

        fixture.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.Is<SessionValidationContext>(
                    c => c.SessionId == sessionId),
                fixture.CancellationToken))
            .ReturnsAsync(validation);

        var result = await fixture.Sut.ValidateAsync(
            fixture.HttpContext,
            fixture.CancellationToken);

        var ok = result.Should()
            .BeOfType<Ok<AuthValidationResult>>()
            .Subject;

        ok.Value.Should().NotBeNull();

        ok.Value!.State.Should()
            .Be(SessionState.Invalid);

        ok.Value.IsValid.Should().BeFalse();

        ok.Value.Snapshot.Should().BeNull();

        fixture.SnapshotFactory.Verify(
            x => x.CreateAsync(
                It.IsAny<SessionValidationResult>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =====================================================================
    // Successful validation
    // =====================================================================

    [Fact]
    public async Task ValidateAsync_WhenSessionIsValid_ReturnsOkActiveWithSnapshot()
    {
        var fixture = CreateFixture();

        var sessionId =
            TestIds.Session("active-session");

        SetupSessionCredential(
            fixture,
            sessionId);

        var validation = CreateActiveValidation(
            fixture.Flow.Tenant,
            sessionId);

        var snapshot = CreateSnapshot(
            fixture.Flow.Tenant,
            validation.UserKey!.Value);

        fixture.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                fixture.CancellationToken))
            .ReturnsAsync(validation);

        fixture.SnapshotFactory
            .Setup(x => x.CreateAsync(
                validation,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        var result = await fixture.Sut.ValidateAsync(
            fixture.HttpContext,
            fixture.CancellationToken);

        var ok = result.Should()
            .BeOfType<Ok<AuthValidationResult>>()
            .Subject;

        ok.Value.Should().NotBeNull();

        var value = ok.Value!;

        value.State.Should()
            .Be(SessionState.Active);

        value.IsValid.Should().BeTrue();

        value.Snapshot.Should()
            .BeSameAs(snapshot);

        value.ChainId.Should()
            .Be(validation.ChainId!);

        value.RootId.Should()
            .Be(validation.RootId!);

        fixture.SnapshotFactory.Verify(
            x => x.CreateAsync(
                validation,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =====================================================================
    // Current invalid-state behavior
    // =====================================================================

    [Fact]
    public async Task ValidateAsync_WhenValidationIsInvalidButContainsUserKey_ReturnsOkWithValidationState()
    {
        var fixture = CreateFixture();

        var sessionId =
            TestIds.Session("revoked-session");

        var userKey =
            UserKey.New();

        SetupSessionCredential(
            fixture,
            sessionId);

        var validation =
            SessionValidationResult.Invalid(
                SessionState.Revoked,
                userKey: userKey,
                sessionId: sessionId);

        fixture.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                fixture.CancellationToken))
            .ReturnsAsync(validation);

        var result = await fixture.Sut.ValidateAsync(
            fixture.HttpContext,
            fixture.CancellationToken);

        var ok = result.Should()
            .BeOfType<Ok<AuthValidationResult>>()
            .Subject;

        ok.Value.Should().NotBeNull();

        var value = ok.Value!;

        value.State.Should()
            .Be(SessionState.Revoked);

        value.IsValid.Should().BeFalse();

        value.Snapshot.Should().BeNull();

        fixture.SnapshotFactory.Verify(
            x => x.CreateAsync(
                It.IsAny<SessionValidationResult>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =====================================================================
    // CancellationToken
    // =====================================================================

    [Fact]
    public async Task ValidateAsync_PropagatesCancellationTokenToSessionValidator()
    {
        var fixture = CreateFixture();

        var sessionId =
            TestIds.Session("cancellation-session");

        SetupSessionCredential(
            fixture,
            sessionId);

        var validation = CreateActiveValidation(
            fixture.Flow.Tenant,
            sessionId);

        fixture.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                fixture.CancellationToken))
            .ReturnsAsync(validation);

        fixture.SnapshotFactory
            .Setup(x => x.CreateAsync(
                validation,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthStateSnapshot?)null);

        await fixture.Sut.ValidateAsync(
            fixture.HttpContext,
            fixture.CancellationToken);

        fixture.SessionValidator.Verify(
            x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                fixture.CancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WhenValidationIsActiveButHasNoUserKey_ReturnsUnauthorizedInvalid()
    {
        var fixture = CreateFixture();

        var sessionId =
            TestIds.Session("active-without-user");

        SetupSessionCredential(
            fixture,
            sessionId);

        var validation =
            SessionValidationResult.Active(
                tenant: fixture.Flow.Tenant,
                userKey: null,
                sessionId: sessionId,
                chainId: SessionChainId.New(),
                rootId: SessionRootId.New(),
                claims: ClaimsSnapshot.Empty,
                authenticatedAt: Now);

        fixture.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                fixture.CancellationToken))
            .ReturnsAsync(validation);

        var result = await fixture.Sut.ValidateAsync(
            fixture.HttpContext,
            fixture.CancellationToken);

        var json = result.Should()
            .BeOfType<JsonHttpResult<AuthValidationResult>>()
            .Subject;

        json.StatusCode.Should()
            .Be(StatusCodes.Status401Unauthorized);

        json.Value.Should().NotBeNull();

        json.Value!.State.Should()
            .Be(SessionState.Invalid);

        json.Value.IsValid.Should().BeFalse();
        json.Value.Snapshot.Should().BeNull();

        fixture.SnapshotFactory.Verify(
            x => x.CreateAsync(
                It.IsAny<SessionValidationResult>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [Fact]
    public async Task ValidateAsync_PureJwt_AuthenticationFails_ReturnsUnauthorizedInvalid()
    {
        var fixture = CreatePureJwtFixture(AuthenticateResult.NoResult());

        var result = await fixture.Sut.ValidateAsync(fixture.HttpContext, TestContext.Current.CancellationToken);

        var json = result.Should()
            .BeOfType<JsonHttpResult<AuthValidationResult>>()
            .Subject;

        json.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        json.Value!.State.Should().Be(SessionState.Invalid);
        json.Value.Snapshot.Should().BeNull();

        fixture.CredentialResolver.VerifyNoOtherCalls();
        fixture.SessionValidator.VerifyNoOtherCalls();
        fixture.SnapshotFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_PureJwt_UnauthenticatedPrincipal_ReturnsUnauthorizedInvalid()
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, UserKey.New().Value)
            }));

        var fixture = CreatePureJwtFixture(JwtSuccess(principal));

        var result = await fixture.Sut.ValidateAsync(fixture.HttpContext, TestContext.Current.CancellationToken);

        var json = result.Should()
            .BeOfType<JsonHttpResult<AuthValidationResult>>()
            .Subject;

        json.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        json.Value!.State.Should().Be(SessionState.Invalid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ValidateAsync_PureJwt_InvalidSubject_ReturnsUnauthorizedInvalid(
        string? subject)
    {
        var claims = subject is null
            ? Array.Empty<Claim>()
            : new[] { new Claim(ClaimTypes.NameIdentifier, subject) };

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                UAuthConstants.SchemeDefaults.GlobalScheme));

        var fixture = CreatePureJwtFixture(JwtSuccess(principal));

        var result = await fixture.Sut.ValidateAsync(fixture.HttpContext, TestContext.Current.CancellationToken);

        var json = result.Should()
            .BeOfType<JsonHttpResult<AuthValidationResult>>()
            .Subject;

        json.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        json.Value!.State.Should().Be(SessionState.Invalid);
        json.Value.Snapshot.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_PureJwt_ValidPrincipal_ReturnsActiveSnapshot()
    {
        var userKey = UserKey.New();

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                new Claim(ClaimTypes.NameIdentifier, userKey.Value),
                new Claim("uauth:permission", "users.read"),
                new Claim("uauth:role", "admin")
                },
                UAuthConstants.SchemeDefaults.GlobalScheme));

        var fixture = CreatePureJwtFixture(JwtSuccess(principal));

        var result = await fixture.Sut.ValidateAsync(fixture.HttpContext, TestContext.Current.CancellationToken);

        var ok = result.Should()
            .BeOfType<Ok<AuthValidationResult>>()
            .Subject;

        ok.Value.Should().NotBeNull();
        ok.Value!.State.Should().Be(SessionState.Active);
        ok.Value.IsValid.Should().BeTrue();

        var snapshot = ok.Value.Snapshot;

        snapshot.Should().NotBeNull();
        snapshot!.Identity.Should().NotBeNull();
        snapshot.Identity!.UserKey.Should().Be(userKey);
        snapshot.Identity.Tenant.Should().Be(fixture.Flow.Tenant);
        snapshot.Identity.SessionState.Should().BeNull();
        snapshot.Identity.UserStatus.Should().Be(UserStatus.Unknown);

        snapshot.Claims.Claims["uauth:permission"]
            .Should().Contain("users.read");

        fixture.CredentialResolver.VerifyNoOtherCalls();
        fixture.SessionValidator.VerifyNoOtherCalls();
        fixture.SnapshotFactory.VerifyNoOtherCalls();
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static Fixture CreateFixture(UAuthMode mode = UAuthMode.PureOpaque)
    {
        var flow = AuthFlowTestFactory.New(mode: mode);

        var authContext =
            new Mock<IAuthFlowContextAccessor>(
                MockBehavior.Strict);

        var credentialResolver =
            new Mock<IValidateCredentialResolver>(
                MockBehavior.Strict);

        var sessionValidator =
            new Mock<ISessionValidator>(
                MockBehavior.Strict);

        var snapshotFactory =
            new Mock<IAuthStateSnapshotFactory>(
                MockBehavior.Strict);

        var clock =
            new Mock<IClock>(
                MockBehavior.Strict);

        authContext
            .SetupGet(x => x.Current)
            .Returns(flow);

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(Now);

        var httpContext =
            new DefaultHttpContext();

        httpContext.Items[UAuthConstants.HttpItems.TenantContextKey] = UAuthTenantContext.Resolved(flow.Tenant);

        var cancellationToken = new CancellationTokenSource().Token;

        var sut = new ValidateEndpointHandler(
            authContext.Object,
            credentialResolver.Object,
            sessionValidator.Object,
            snapshotFactory.Object,
            clock.Object);

        return new Fixture(
            sut,
            flow,
            credentialResolver,
            sessionValidator,
            snapshotFactory,
            httpContext,
            cancellationToken);
    }

    private static Fixture CreatePureJwtFixture(AuthenticateResult authenticationResult)
    {
        var fixture = CreateFixture(UAuthMode.PureJwt);

        var authentication = new Mock<IAuthenticationService>();

        authentication
            .Setup(x => x.AuthenticateAsync(
                It.IsAny<HttpContext>(),
                UAuthConstants.SchemeDefaults.GlobalScheme))
            .ReturnsAsync(authenticationResult);

        fixture.HttpContext.RequestServices =
            new ServiceCollection()
                .AddSingleton(authentication.Object)
                .BuildServiceProvider();

        return fixture;
    }

    private static AuthenticateResult JwtSuccess(ClaimsPrincipal principal)
    {
        return AuthenticateResult.Success(
            new AuthenticationTicket(
                principal,
                UAuthConstants.SchemeDefaults.GlobalScheme));
    }


    private static void SetupSessionCredential(Fixture fixture, AuthSessionId sessionId)
    {
        fixture.CredentialResolver
            .Setup(x => x.ResolveAsync(
                fixture.HttpContext,
                fixture.Flow.Response))
            .ReturnsAsync(
                new ResolvedCredential
                {
                    Kind = PrimaryTokenKind.Session,
                    Value = sessionId.Value,
                    Tenant = fixture.Flow.Tenant,
                    Device = TestDevice.DefaultDeviceInfo()
                });
    }

    private static SessionValidationResult CreateActiveValidation(TenantKey tenant, AuthSessionId sessionId)
    {
        return SessionValidationResult.Active(
            tenant: tenant,
            userKey: UserKey.New(),
            sessionId: sessionId,
            chainId: SessionChainId.New(),
            rootId: SessionRootId.New(),
            claims: ClaimsSnapshot.Empty,
            authenticatedAt: Now);
    }

    private static AuthStateSnapshot CreateSnapshot(TenantKey tenant, UserKey userKey)
    {
        return new AuthStateSnapshot
        {
            Identity = new AuthIdentitySnapshot
            {
                UserKey = userKey,
                Tenant = tenant,
                AuthenticatedAt = Now,
                SessionState = SessionState.Active,
                UserStatus = UserStatus.Unknown
            },
            Claims = ClaimsSnapshot.Empty
        };
    }

    private sealed record Fixture(
        ValidateEndpointHandler Sut,
        AuthFlowContext Flow,
        Mock<IValidateCredentialResolver> CredentialResolver,
        Mock<ISessionValidator> SessionValidator,
        Mock<IAuthStateSnapshotFactory> SnapshotFactory,
        DefaultHttpContext HttpContext,
        CancellationToken CancellationToken);
}
