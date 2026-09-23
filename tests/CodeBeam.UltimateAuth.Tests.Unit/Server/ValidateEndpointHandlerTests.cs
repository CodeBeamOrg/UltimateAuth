using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

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
            Device = null
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
            Device = null
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
    public async Task ValidateAsync_WhenValidationHasNoUserKey_ReturnsUnauthorizedInvalid()
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
                userId: null,
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

        var json = result.Should()
            .BeOfType<JsonHttpResult<AuthValidationResult>>()
            .Subject;

        json.StatusCode.Should()
            .Be(StatusCodes.Status401Unauthorized);

        json.Value.Should().NotBeNull();
        json.Value!.State.Should()
            .Be(SessionState.Invalid);

        json.Value.Snapshot.Should().BeNull();

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

        ok.Value!.State.Should()
            .Be(SessionState.Active);

        ok.Value.Snapshot.Should()
            .BeSameAs(snapshot);

        fixture.SnapshotFactory.Verify(x => x.CreateAsync(
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
                userId: userKey,
                sessionId: sessionId);

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

        var result = await fixture.Sut.ValidateAsync(
            fixture.HttpContext,
            fixture.CancellationToken);

        var ok = result.Should()
            .BeOfType<Ok<AuthValidationResult>>()
            .Subject;

        ok.Value.Should().NotBeNull();

        ok.Value!.State.Should()
            .Be(SessionState.Revoked);

        ok.Value.Snapshot.Should().BeNull();

        fixture.SnapshotFactory.Verify(x => x.CreateAsync(
            validation,
            It.IsAny<CancellationToken>()),
            Times.Once);
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

    // =====================================================================
    // Helpers
    // =====================================================================

    private static Fixture CreateFixture()
    {
        var flow =
            AuthFlowTestFactory.LoginSuccess();

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

    private static void SetupSessionCredential(
        Fixture fixture,
        AuthSessionId sessionId)
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
                    Device = null
                });
    }

    private static SessionValidationResult CreateActiveValidation(
        TenantKey tenant,
        AuthSessionId sessionId)
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

    private static AuthStateSnapshot CreateSnapshot(
        TenantKey tenant,
        UserKey userKey)
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