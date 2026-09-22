using System.Text;
using System.Text.Json;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Server.Stores;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class PkceEndpointHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    // ---------------------------------------------------------------------
    // Authorize
    // ---------------------------------------------------------------------

    [Fact]
    public async Task AuthorizeAsync_WhenContentTypeIsInvalid_ReturnsBadRequest()
    {
        var fixture = CreateFixture();

        var result = await fixture.Sut.AuthorizeAsync(fixture.HttpContext);

        var badRequest = result.Should()
            .BeOfType<BadRequest<string>>()
            .Subject;

        badRequest.Value.Should().Be("Invalid content type.");

        fixture.PkceService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AuthorizeAsync_WithJsonRequest_ForwardsCommandAndReturnsAuthorizationCode()
    {
        var fixture = CreateFixture();

        var device = TestDevice.Default();

        SetJsonBody(fixture.HttpContext, new
        {
            codeChallenge = "challenge-123",
            challengeMethod = "S256",
            redirectUri = "/after-login",
            device
        });

        PkceAuthorizeCommand? captured = null;

        fixture.PkceService
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<PkceAuthorizeCommand>(),
                fixture.HttpContext.RequestAborted))
            .Callback<PkceAuthorizeCommand, CancellationToken>(
                (command, _) => captured = command)
            .ReturnsAsync(new PkceAuthorizeResponse
            {
                AuthorizationCode = "authorization-code",
                ExpiresIn = 300
            });

        var result = await fixture.Sut.AuthorizeAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<PkceAuthorizeResponse>>()
            .Subject;

        ok.Value.Should().NotBeNull();
        ok.Value!.AuthorizationCode.Should().Be("authorization-code");
        ok.Value.ExpiresIn.Should().Be(300);

        captured.Should().NotBeNull();
        captured!.CodeChallenge.Should().Be("challenge-123");
        captured.ChallengeMethod.Should().Be("S256");
        captured.RedirectUri.Should().Be("/after-login");
        captured.Device.Should().BeEquivalentTo(device);
        captured.ClientProfile.Should().Be(fixture.Flow.ClientProfile);
        captured.Tenant.Should().Be(fixture.Flow.Tenant);
    }

    // ---------------------------------------------------------------------
    // TryComplete
    // ---------------------------------------------------------------------

    [Fact]
    public async Task TryCompleteAsync_WhenFlowIsNotLogin_ReturnsBadRequest()
    {
        var fixture = CreateFixture(CreateNonLoginFlow());

        var result = await fixture.Sut.TryCompleteAsync(
            fixture.HttpContext);

        var badRequest = result.Should()
            .BeOfType<BadRequest<string>>()
            .Subject;

        badRequest.Value.Should()
            .Be("PKCE is only supported for login flow.");

        fixture.AuthStore.VerifyNoOtherCalls();
        fixture.Validator.VerifyNoOtherCalls();
        fixture.InternalFlow.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryCompleteAsync_WhenPayloadContentTypeIsInvalid_ReturnsBadRequest()
    {
        var fixture = CreateFixture();

        var result = await fixture.Sut.TryCompleteAsync(
            fixture.HttpContext);

        var badRequest = result.Should()
            .BeOfType<BadRequest<string>>()
            .Subject;

        badRequest.Value.Should().Be("Invalid PKCE payload.");

        fixture.AuthStore.VerifyNoOtherCalls();
        fixture.Validator.VerifyNoOtherCalls();
        fixture.InternalFlow.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryCompleteAsync_WhenAuthorizationCodeIsMissing_ReturnsBadRequest()
    {
        var fixture = CreateFixture();

        SetJsonBody(fixture.HttpContext, new
        {
            authorization_code = "",
            code_verifier = "verifier",
            identifier = "user",
            secret = "password"
        });

        var result = await fixture.Sut.TryCompleteAsync(fixture.HttpContext);

        var badRequest = result.Should()
            .BeOfType<BadRequest<string>>()
            .Subject;

        badRequest.Value.Should()
            .Be("authorization_code and code_verifier are required.");

        fixture.AuthStore.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryCompleteAsync_WhenCodeVerifierIsMissing_ReturnsBadRequest()
    {
        var fixture = CreateFixture();

        SetJsonBody(fixture.HttpContext, new
        {
            authorization_code = "code",
            code_verifier = "",
            identifier = "user",
            secret = "password"
        });

        var result = await fixture.Sut.TryCompleteAsync(fixture.HttpContext);

        result.Should().BeOfType<BadRequest<string>>();

        fixture.AuthStore.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryCompleteAsync_WhenArtifactDoesNotExist_RequestsNewPkce()
    {
        var fixture = CreateFixture();

        SetCompleteJson(fixture, "missing-code", "verifier");

        fixture.AuthStore
            .Setup(x => x.GetAsync(
                new AuthArtifactKey("missing-code"),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync((AuthArtifact?)null);

        var result = await fixture.Sut.TryCompleteAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<TryPkceLoginResult>>()
            .Subject;

        ok.Value.Should().NotBeNull();
        ok.Value!.Success.Should().BeFalse();
        ok.Value.RetryWithNewPkce.Should().BeTrue();

        fixture.Validator.VerifyNoOtherCalls();
        fixture.InternalFlow.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryCompleteAsync_WhenArtifactIsNotPkceArtifact_RequestsNewPkce()
    {
        var fixture = CreateFixture();
        var hub = TestHubFactory.Create(TestPkceFactory.Create().Artifact);

        SetCompleteJson(fixture, "code", "verifier");

        fixture.AuthStore
            .Setup(x => x.GetAsync(
                new AuthArtifactKey("code"),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(hub);

        var result = await fixture.Sut.TryCompleteAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<TryPkceLoginResult>>()
            .Subject;

        ok.Value!.Success.Should().BeFalse();
        ok.Value.RetryWithNewPkce.Should().BeTrue();

        fixture.Validator.VerifyNoOtherCalls();
        fixture.InternalFlow.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryCompleteAsync_WhenPkceValidationFails_RequestsNewPkce()
    {
        var fixture = CreateFixture();
        var (artifact, _) = TestPkceFactory.Create();

        SetCompleteJson(
            fixture,
            artifact.AuthorizationCode.Value,
            "wrong-verifier");

        fixture.AuthStore
            .Setup(x => x.GetAsync(
                artifact.AuthorizationCode,
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(artifact);

        fixture.Validator
            .Setup(x => x.Validate(
                artifact,
                "wrong-verifier",
                It.IsAny<PkceContextSnapshot>(),
                Now))
            .Returns(PkceValidationResult.Fail(
                PkceValidationFailureReason.InvalidVerifier));

        var result = await fixture.Sut.TryCompleteAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<TryPkceLoginResult>>()
            .Subject;

        ok.Value!.Success.Should().BeFalse();
        ok.Value.RetryWithNewPkce.Should().BeTrue();

        fixture.InternalFlow.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryCompleteAsync_WhenPkceIsValid_UsesArtifactContextAndPreviewLogin()
    {
        var fixture = CreateFixture();
        var (artifact, verifier) = TestPkceFactory.Create();

        SetCompleteJson(
            fixture,
            artifact.AuthorizationCode.Value,
            verifier,
            identifier: "alice",
            secret: "secret");

        fixture.AuthStore
            .Setup(x => x.GetAsync(
                artifact.AuthorizationCode,
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(artifact);

        PkceContextSnapshot? capturedSnapshot = null;

        fixture.Validator
            .Setup(x => x.Validate(
                artifact,
                verifier,
                It.IsAny<PkceContextSnapshot>(),
                Now))
            .Callback<PkceAuthorizationArtifact,
                string,
                PkceContextSnapshot,
                DateTimeOffset>(
                    (_, _, snapshot, _) =>
                        capturedSnapshot = snapshot)
            .Returns(PkceValidationResult.Ok());

        AuthExecutionContext? capturedExecution = null;
        LoginRequest? capturedLogin = null;
        LoginExecutionOptions? capturedOptions = null;

        fixture.InternalFlow
            .Setup(x => x.LoginAsync(
                fixture.Flow,
                It.IsAny<AuthExecutionContext>(),
                It.IsAny<LoginRequest>(),
                It.IsAny<LoginExecutionOptions>(),
                fixture.HttpContext.RequestAborted))
            .Callback<AuthFlowContext,
                AuthExecutionContext,
                LoginRequest,
                LoginExecutionOptions,
                CancellationToken>(
                    (_, execution, request, options, _) =>
                    {
                        capturedExecution = execution;
                        capturedLogin = request;
                        capturedOptions = options;
                    })
            .ReturnsAsync(LoginResult.SuccessPreview());

        var result = await fixture.Sut.TryCompleteAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<TryPkceLoginResult>>()
            .Subject;

        ok.Value!.Success.Should().BeTrue();
        ok.Value.RetryWithNewPkce.Should().BeFalse();

        capturedSnapshot.Should().NotBeNull();
        capturedSnapshot!.ClientProfile
            .Should().Be(artifact.Context.ClientProfile);
        capturedSnapshot.Tenant
            .Should().Be(artifact.Context.Tenant);
        capturedSnapshot.RedirectUri
            .Should().Be(artifact.Context.RedirectUri);
        capturedSnapshot.Device
            .Should().BeEquivalentTo(artifact.Context.Device);

        capturedExecution.Should().NotBeNull();
        capturedExecution!.EffectiveClientProfile
            .Should().Be(artifact.Context.ClientProfile);
        capturedExecution.Device
            .Should().BeEquivalentTo(artifact.Context.Device);

        capturedLogin.Should().NotBeNull();
        capturedLogin!.Identifier.Should().Be("alice");
        capturedLogin.Secret.Should().Be("secret");
        capturedLogin.RequestTokens
            .Should().Be(fixture.Flow.AllowsTokenIssuance);

        capturedOptions.Should().NotBeNull();
        capturedOptions!.Mode.Should().Be(LoginExecutionMode.Preview);
        capturedOptions.SuppressFailureAttempt.Should().BeFalse();
        capturedOptions.SuppressSuccessReset.Should().BeTrue();
    }

    [Fact]
    public async Task TryCompleteAsync_WhenPreviewFails_ForwardsFailureInformation()
    {
        var fixture = CreateFixture();
        var (artifact, verifier) = TestPkceFactory.Create();

        SetCompleteJson(
            fixture,
            artifact.AuthorizationCode.Value,
            verifier);

        SetupValidPkce(fixture, artifact, verifier);

        var lockoutUntil = Now.AddMinutes(10);

        fixture.InternalFlow
            .Setup(x => x.LoginAsync(
                fixture.Flow,
                It.IsAny<AuthExecutionContext>(),
                It.IsAny<LoginRequest>(),
                It.IsAny<LoginExecutionOptions>(),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(LoginResult.Failed(
                AuthFailureReason.InvalidCredentials,
                lockoutUntil,
                remainingAttempts: 2));

        var result = await fixture.Sut.TryCompleteAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<TryPkceLoginResult>>()
            .Subject;

        ok.Value!.Success.Should().BeFalse();
        ok.Value.Reason.Should().Be(AuthFailureReason.InvalidCredentials);
        ok.Value.RemainingAttempts.Should().Be(2);
        ok.Value.LockoutUntilUtc.Should().Be(lockoutUntil);
        ok.Value.RequiresMfa.Should().BeFalse();
        ok.Value.RetryWithNewPkce.Should().BeFalse();
    }

    [Fact]
    public async Task TryCompleteAsync_WhenPreviewRequiresMfa_SetsRequiresMfa()
    {
        var fixture = CreateFixture();
        var (artifact, verifier) = TestPkceFactory.Create();

        SetCompleteJson(
            fixture,
            artifact.AuthorizationCode.Value,
            verifier);

        SetupValidPkce(fixture, artifact, verifier);

        fixture.InternalFlow
            .Setup(x => x.LoginAsync(
                fixture.Flow,
                It.IsAny<AuthExecutionContext>(),
                It.IsAny<LoginRequest>(),
                It.IsAny<LoginExecutionOptions>(),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(LoginResult.Failed(
                AuthFailureReason.RequiresMfa));

        var result = await fixture.Sut.TryCompleteAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<TryPkceLoginResult>>()
            .Subject;

        ok.Value!.Success.Should().BeFalse();
        ok.Value.RequiresMfa.Should().BeTrue();
        ok.Value.RetryWithNewPkce.Should().BeFalse();
    }

    // ---------------------------------------------------------------------
    // Complete
    // ---------------------------------------------------------------------

    [Fact]
    public async Task CompleteAsync_WhenPayloadContentTypeIsInvalid_ReturnsBadRequest()
    {
        var fixture = CreateFixture();

        var result = await fixture.Sut.CompleteAsync(
            fixture.HttpContext);

        result.Should().BeOfType<BadRequest<string>>();

        fixture.PkceService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteAsync_WhenRequiredPkceValuesAreMissing_ReturnsBadRequest()
    {
        var fixture = CreateFixture();

        SetJsonBody(fixture.HttpContext, new
        {
            authorization_code = "",
            code_verifier = "",
            identifier = "alice",
            secret = "secret"
        });

        var result = await fixture.Sut.CompleteAsync(fixture.HttpContext);

        result.Should().BeOfType<BadRequest<string>>();

        fixture.PkceService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteAsync_WhenPkceIsInvalid_ReturnsUnauthorized()
    {
        var fixture = CreateFixture();

        SetCompleteJson(fixture, "code", "verifier");

        fixture.PkceService
            .Setup(x => x.CompleteAsync(
                fixture.Flow,
                It.IsAny<PkceCompleteRequest>(),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(new PkceCompleteResult
            {
                InvalidPkce = true
            });

        var result = await fixture.Sut.CompleteAsync(
            fixture.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        fixture.CredentialWriter.VerifyNoOtherCalls();
        fixture.RedirectResolver.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteAsync_WhenLoginFailsWithoutHub_RedirectsToLogin()
    {
        var fixture = CreateFixture();

        SetCompleteJson(fixture, "code", "verifier");

        fixture.PkceService
            .Setup(x => x.CompleteAsync(
                fixture.Flow,
                It.IsAny<PkceCompleteRequest>(),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(new PkceCompleteResult
            {
                Success = false,
                InvalidPkce = false
            });

        var result = await fixture.Sut.CompleteAsync(
            fixture.HttpContext);

        var redirect = result.Should()
            .BeOfType<RedirectHttpResult>()
            .Subject;

        redirect.Url.Should().Be(
            fixture.Flow.OriginalOptions.Hub.LoginPath ?? "/login");

        fixture.CredentialWriter.VerifyNoOtherCalls();
        fixture.RedirectResolver.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteAsync_WhenLoginFailsWithHub_SetsHubErrorAndPreservesHubKey()
    {
        var fixture = CreateFixture();
        var (pkceArtifact, _) = TestPkceFactory.Create();
        var hub = TestHubFactory.Create(pkceArtifact);
        var hubKey = hub.HubSessionId.Value;

        fixture.HttpContext.Request.QueryString =
            new QueryString($"?uauth_hub={Uri.EscapeDataString(hubKey)}");

        SetCompleteJson(fixture, "code", "verifier");

        fixture.PkceService
            .Setup(x => x.CompleteAsync(
                fixture.Flow,
                It.IsAny<PkceCompleteRequest>(),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(new PkceCompleteResult
            {
                Success = false
            });

        fixture.AuthStore
            .Setup(x => x.GetAsync(
                new AuthArtifactKey(hubKey),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(hub);

        fixture.AuthStore
            .Setup(x => x.StoreAsync(
                new AuthArtifactKey(hubKey),
                hub,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await fixture.Sut.CompleteAsync(
            fixture.HttpContext);

        hub.Error.Should().Be(HubErrorCode.InvalidCredentials);

        var redirect = result.Should()
            .BeOfType<RedirectHttpResult>()
            .Subject;

        redirect.Url.Should().Contain("hub=");
        redirect.Url.Should().Contain(
            Uri.EscapeDataString(hubKey));

        fixture.AuthStore.Verify(x => x.StoreAsync(
            new AuthArtifactKey(hubKey),
            hub,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_WhenSuccessfulAndRedirectDisabled_ReturnsOk()
    {
        var fixture = CreateFixture();

        SetCompleteJson(fixture, "code", "verifier");

        var sessionId = TestIds.Session("test-session");

        fixture.PkceService
            .Setup(x => x.CompleteAsync(
                fixture.Flow,
                It.IsAny<PkceCompleteRequest>(),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(new PkceCompleteResult
            {
                Success = true,
                LoginResult = LoginResult.Success(sessionId)
            });

        fixture.CredentialWriter
            .Setup(x => x.Write(
                fixture.HttpContext,
                GrantKind.Session,
                sessionId));

        fixture.RedirectResolver
            .Setup(x => x.ResolveSuccess(
                fixture.Flow,
                fixture.HttpContext))
            .Returns(RedirectDecision.None());

        var result = await fixture.Sut.CompleteAsync(
            fixture.HttpContext);

        result.Should().BeOfType<Ok>();

        fixture.CredentialWriter.Verify(x => x.Write(
            fixture.HttpContext,
            GrantKind.Session,
            sessionId),
            Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_WhenSuccessfulAndRedirectEnabled_ReturnsRedirect()
    {
        var fixture = CreateFixture();

        SetCompleteJson(fixture, "code", "verifier");

        var sessionId = TestIds.Session("test-session");

        fixture.PkceService
            .Setup(x => x.CompleteAsync(
                fixture.Flow,
                It.IsAny<PkceCompleteRequest>(),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(new PkceCompleteResult
            {
                Success = true,
                LoginResult = LoginResult.Success(sessionId)
            });

        fixture.CredentialWriter
            .Setup(x => x.Write(
                fixture.HttpContext,
                GrantKind.Session,
                sessionId));

        fixture.RedirectResolver
            .Setup(x => x.ResolveSuccess(
                fixture.Flow,
                fixture.HttpContext))
            .Returns(RedirectDecision.To("/app"));

        var result = await fixture.Sut.CompleteAsync(
            fixture.HttpContext);

        var redirect = result.Should()
            .BeOfType<RedirectHttpResult>()
            .Subject;

        redirect.Url.Should().Be("/app");
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static Fixture CreateFixture(
        AuthFlowContext? flow = null)
    {
        flow ??= AuthFlowTestFactory.LoginSuccess();

        var authContext =
            new Mock<IAuthFlowContextAccessor>(MockBehavior.Strict);

        var publicFlow =
            new Mock<IUAuthFlowService>(MockBehavior.Strict);

        var pkceService =
            new Mock<IPkceService>(MockBehavior.Strict);

        var internalFlow =
            new Mock<IUAuthInternalFlowService>(MockBehavior.Strict);

        var authStore =
            new Mock<IAuthStore>(MockBehavior.Strict);

        var validator =
            new Mock<IPkceAuthorizationValidator>(MockBehavior.Strict);

        var clock =
            new Mock<IClock>(MockBehavior.Strict);

        var credentialWriter =
            new Mock<ICredentialResponseWriter>(MockBehavior.Strict);

        var redirectResolver =
            new Mock<IAuthRedirectResolver>(MockBehavior.Strict);

        authContext
            .SetupGet(x => x.Current)
            .Returns(flow);

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(Now);

        var httpContext = new DefaultHttpContext();

        var sut = new PkceEndpointHandler(
            authContext.Object,
            publicFlow.Object,
            pkceService.Object,
            internalFlow.Object,
            authStore.Object,
            validator.Object,
            clock.Object,
            credentialWriter.Object,
            redirectResolver.Object);

        return new Fixture(
            sut,
            flow,
            pkceService,
            internalFlow,
            authStore,
            validator,
            credentialWriter,
            redirectResolver,
            httpContext);
    }

    private static void SetupValidPkce(
        Fixture fixture,
        PkceAuthorizationArtifact artifact,
        string verifier)
    {
        fixture.AuthStore
            .Setup(x => x.GetAsync(
                artifact.AuthorizationCode,
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(artifact);

        fixture.Validator
            .Setup(x => x.Validate(
                artifact,
                verifier,
                It.IsAny<PkceContextSnapshot>(),
                Now))
            .Returns(PkceValidationResult.Ok());
    }

    private static void SetCompleteJson(
    Fixture fixture,
    string authorizationCode,
    string codeVerifier,
    string identifier = "alice",
    string secret = "secret")
    {
        SetJsonBody(fixture.HttpContext, new
        {
            authorization_code = authorizationCode,
            code_verifier = codeVerifier,
            identifier,
            secret
        });
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

    private static AuthFlowContext CreateNonLoginFlow()
    {
        var source = AuthFlowTestFactory.LoginSuccess();

        return new AuthFlowContext(
            flowType: AuthFlowType.RefreshSession,
            clientProfile: source.ClientProfile,
            effectiveMode: source.EffectiveMode,
            device: source.Device,
            tenantKey: source.Tenant,
            isAuthenticated: source.IsAuthenticated,
            userKey: source.UserKey,
            session: source.Session,
            originalOptions: source.OriginalOptions,
            effectiveOptions: source.EffectiveOptions,
            response: source.Response,
            primaryTokenKind: source.PrimaryTokenKind,
            returnUrlInfo: source.ReturnUrlInfo);
    }

    private sealed record Fixture(
        PkceEndpointHandler Sut,
        AuthFlowContext Flow,
        Mock<IPkceService> PkceService,
        Mock<IUAuthInternalFlowService> InternalFlow,
        Mock<IAuthStore> AuthStore,
        Mock<IPkceAuthorizationValidator> Validator,
        Mock<ICredentialResponseWriter> CredentialWriter,
        Mock<IAuthRedirectResolver> RedirectResolver,
        DefaultHttpContext HttpContext);
}