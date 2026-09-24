using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Server.Stores;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class LoginEndpointHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    // =====================================================================
    // TryLoginAsync - request validation
    // =====================================================================

    [Fact]
    public async Task TryLoginAsync_WhenContentTypeIsUnsupported_ReturnsBadRequest()
    {
        var fixture = CreateFixture();

        var result = await fixture.Sut.TryLoginAsync(
            fixture.HttpContext);

        var badRequest = result.Should()
            .BeOfType<BadRequest<string>>()
            .Subject;

        badRequest.Value.Should().Be("Invalid content type.");

        fixture.InternalFlow.VerifyNoOtherCalls();
        fixture.AuthStore.VerifyNoOtherCalls();
        fixture.IdentifierResolver.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("alice", "")]
    [InlineData(" ", "password")]
    [InlineData("alice", " ")]
    public async Task TryLoginAsync_WhenCredentialsAreMissing_ReturnsInvalidCredentials(
        string identifier,
        string secret)
    {
        var fixture = CreateFixture();

        SetLoginJson(
            fixture.HttpContext,
            identifier,
            secret);

        var result = await fixture.Sut.TryLoginAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<TryLoginResult>>()
            .Subject;

        ok.Value.Should().NotBeNull();
        ok.Value!.Success.Should().BeFalse();
        ok.Value.Reason.Should()
            .Be(AuthFailureReason.InvalidCredentials);

        fixture.InternalFlow.VerifyNoOtherCalls();
        fixture.AuthStore.VerifyNoOtherCalls();
        fixture.IdentifierResolver.VerifyNoOtherCalls();
    }

    // =====================================================================
    // TryLoginAsync - preview execution
    // =====================================================================

    [Fact]
    public async Task TryLoginAsync_ExecutesPreviewWithExpectedRequestAndOptions()
    {
        var fixture = CreateFixture();

        SetLoginJson(
            fixture.HttpContext,
            identifier: "alice",
            secret: "password");

        LoginRequest? capturedRequest = null;
        LoginExecutionOptions? capturedOptions = null;

        fixture.InternalFlow
            .Setup(x => x.LoginAsync(
                fixture.Flow,
                It.IsAny<LoginRequest>(),
                It.IsAny<LoginExecutionOptions>(),
                fixture.HttpContext.RequestAborted))
            .Callback<AuthFlowContext,
                LoginRequest,
                LoginExecutionOptions,
                CancellationToken>(
                    (_, request, options, _) =>
                    {
                        capturedRequest = request;
                        capturedOptions = options;
                    })
            .ReturnsAsync(
                LoginResult.Failed(
                    AuthFailureReason.InvalidCredentials));

        var result = await fixture.Sut.TryLoginAsync(
            fixture.HttpContext);

        result.Should()
            .BeOfType<Ok<TryLoginResult>>();

        capturedRequest.Should().NotBeNull();

        capturedRequest!.Identifier.Should().Be("alice");
        capturedRequest.Secret.Should().Be("password");
        capturedRequest.Factor.Should()
            .Be(CredentialType.Password);

        capturedRequest.RequestTokens.Should()
            .Be(fixture.Flow.AllowsTokenIssuance);

        capturedOptions.Should().NotBeNull();

        capturedOptions!.Mode.Should()
            .Be(LoginExecutionMode.Preview);

        capturedOptions.SuppressFailureAttempt.Should()
            .BeFalse();

        capturedOptions.SuppressSuccessReset.Should()
            .BeTrue();
    }

    [Fact]
    public async Task TryLoginAsync_WhenPreviewFails_ForwardsFailureInformation()
    {
        var fixture = CreateFixture();

        var lockoutUntil = Now.AddMinutes(5);

        SetLoginJson(
            fixture.HttpContext,
            identifier: "alice",
            secret: "wrong-password");

        var failure = LoginResult.Failed(
            AuthFailureReason.InvalidCredentials,
            lockoutUntilUtc: lockoutUntil,
            remainingAttempts: 2);

        fixture.InternalFlow
            .Setup(x => x.LoginAsync(
                fixture.Flow,
                It.IsAny<LoginRequest>(),
                It.Is<LoginExecutionOptions>(
                    o => o.Mode == LoginExecutionMode.Preview),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(failure);

        var result = await fixture.Sut.TryLoginAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<TryLoginResult>>()
            .Subject;

        ok.Value.Should().NotBeNull();

        ok.Value!.Success.Should().BeFalse();

        ok.Value.Reason.Should()
            .Be(AuthFailureReason.InvalidCredentials);

        ok.Value.RemainingAttempts.Should()
            .Be(2);

        ok.Value.LockoutUntilUtc.Should()
            .Be(lockoutUntil);

        ok.Value.RequiresMfa.Should()
            .BeFalse();

        ok.Value.PreviewReceipt.Should()
            .BeNull();

        fixture.AuthStore.VerifyNoOtherCalls();
        fixture.IdentifierResolver.VerifyNoOtherCalls();
    }

    // =====================================================================
    // TryLoginAsync - preview receipt
    // =====================================================================

    [Fact]
    public async Task TryLoginAsync_WhenPreviewSucceedsAndIdentifierResolves_StoresPreviewArtifact()
    {
        var fixture = CreateFixture();

        var userKey = UserKey.New();

        SetLoginJson(
            fixture.HttpContext,
            identifier: "alice",
            secret: "password");

        fixture.InternalFlow
            .Setup(x => x.LoginAsync(
                fixture.Flow,
                It.IsAny<LoginRequest>(),
                It.Is<LoginExecutionOptions>(
                    o =>
                        o.Mode == LoginExecutionMode.Preview &&
                        !o.SuppressFailureAttempt &&
                        o.SuppressSuccessReset),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(LoginResult.SuccessPreview());

        fixture.IdentifierResolver
            .Setup(x => x.ResolveAsync(
                fixture.Flow.Tenant,
                "alice",
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(
                CreateIdentifierResolution(
                    fixture.Flow.Tenant,
                    userKey,
                    "alice"));

        AuthArtifactKey? storedKey = null;
        LoginPreviewArtifact? storedArtifact = null;

        fixture.AuthStore
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<LoginPreviewArtifact>(),
                fixture.HttpContext.RequestAborted))
            .Callback<AuthArtifactKey,
                AuthArtifact,
                CancellationToken>(
                    (key, artifact, _) =>
                    {
                        storedKey = key;
                        storedArtifact = artifact.Should()
                            .BeOfType<LoginPreviewArtifact>()
                            .Subject;
                    })
            .Returns(Task.CompletedTask);

        var result = await fixture.Sut.TryLoginAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<TryLoginResult>>()
            .Subject;

        ok.Value.Should().NotBeNull();
        ok.Value!.Success.Should().BeTrue();

        ok.Value.PreviewReceipt.Should()
            .NotBeNullOrWhiteSpace();

        storedKey.Should().NotBeNull();

        storedKey!.Value.Should()
            .Be(ok.Value.PreviewReceipt);

        storedArtifact.Should().NotBeNull();

        fixture.AuthStore.Verify(x => x.StoreAsync(
            It.IsAny<AuthArtifactKey>(),
            It.IsAny<LoginPreviewArtifact>(),
            fixture.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task TryLoginAsync_WhenPreviewSucceedsButIdentifierDoesNotResolve_DoesNotStoreArtifact()
    {
        var fixture = CreateFixture();

        SetLoginJson(
            fixture.HttpContext,
            identifier: "alice",
            secret: "password");

        fixture.InternalFlow
            .Setup(x => x.LoginAsync(
                fixture.Flow,
                It.IsAny<LoginRequest>(),
                It.IsAny<LoginExecutionOptions>(),
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(LoginResult.SuccessPreview());

        fixture.IdentifierResolver
            .Setup(x => x.ResolveAsync(
                fixture.Flow.Tenant,
                "alice",
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync((LoginIdentifierResolution?)null);

        var result = await fixture.Sut.TryLoginAsync(
            fixture.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<TryLoginResult>>()
            .Subject;

        ok.Value.Should().NotBeNull();
        ok.Value!.Success.Should().BeTrue();

        fixture.AuthStore.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<LoginPreviewArtifact>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =====================================================================
    // LoginAsync - preview receipt consumption
    // =====================================================================

    [Fact]
    public async Task LoginAsync_WhenPreviewReceiptMatches_ConsumesReceiptAndSuppressesFailureAttempt()
    {
        var fixture = CreateFixture();

        const string receiptValue = "preview-receipt";
        const string identifier = "alice";
        const string secret = "password";

        SetLoginJson(
            fixture.HttpContext,
            identifier,
            secret,
            previewReceipt: receiptValue);

        fixture.Flow.Device.DeviceId.Should().NotBeNull();

        var deviceId = fixture.Flow.Device.DeviceId!.Value;
        var userKey = UserKey.New();

        var fingerprint = LoginPreviewFingerprint.Create(
            fixture.Flow.Tenant,
            identifier,
            CredentialType.Password,
            secret,
            deviceId);

        var artifact = new LoginPreviewArtifact(
            fixture.Flow.Tenant,
            userKey,
            CredentialType.Password,
            deviceId.Value,
            identifier,
            fixture.Flow.ClientProfile,
            fingerprint,
            Now.AddMinutes(5));

        var key = new AuthArtifactKey(receiptValue);

        fixture.AuthStore
            .Setup(x => x.GetAsync(
                key,
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(artifact);

        fixture.AuthStore
            .Setup(x => x.ConsumeAsync(
                key,
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(artifact);

        LoginExecutionOptions? capturedOptions = null;

        fixture.InternalFlow
            .Setup(x => x.LoginAsync(
                fixture.Flow,
                It.IsAny<LoginRequest>(),
                It.IsAny<LoginExecutionOptions>(),
                fixture.HttpContext.RequestAborted))
            .Callback<AuthFlowContext,
                LoginRequest,
                LoginExecutionOptions,
                CancellationToken>(
                    (_, _, options, _) =>
                    {
                        capturedOptions = options;
                    })
            .ReturnsAsync(LoginResult.SuccessPreview());

        fixture.RedirectResolver
            .Setup(x => x.ResolveSuccess(
                fixture.Flow,
                fixture.HttpContext))
            .Returns(RedirectDecision.None());

        var result = await fixture.Sut.LoginAsync(
            fixture.HttpContext);

        result.Should().BeOfType<Ok>();

        capturedOptions.Should().NotBeNull();

        capturedOptions!.Mode.Should()
            .Be(LoginExecutionMode.Commit);

        capturedOptions.SuppressFailureAttempt.Should()
            .BeTrue();

        capturedOptions.SuppressSuccessReset.Should()
            .BeFalse();

        fixture.AuthStore.Verify(x => x.ConsumeAsync(
            key,
            fixture.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenPreviewReceiptDoesNotMatch_DoesNotConsumeAndDoesNotSuppress()
    {
        var fixture = CreateFixture();

        const string receiptValue = "preview-receipt";
        const string identifier = "alice";

        SetLoginJson(
            fixture.HttpContext,
            identifier,
            secret: "different-password",
            previewReceipt: receiptValue);

        fixture.Flow.Device.DeviceId.Should().NotBeNull();

        var deviceId = fixture.Flow.Device.DeviceId!.Value;

        var fingerprint = LoginPreviewFingerprint.Create(
            fixture.Flow.Tenant,
            identifier,
            CredentialType.Password,
            "original-password",
            deviceId);

        var artifact = new LoginPreviewArtifact(
            fixture.Flow.Tenant,
            UserKey.New(),
            CredentialType.Password,
            deviceId.Value,
            identifier,
            fixture.Flow.ClientProfile,
            fingerprint,
            Now.AddMinutes(5));

        var key = new AuthArtifactKey(receiptValue);

        fixture.AuthStore
            .Setup(x => x.GetAsync(
                key,
                fixture.HttpContext.RequestAborted))
            .ReturnsAsync(artifact);

        LoginExecutionOptions? capturedOptions = null;

        fixture.InternalFlow
            .Setup(x => x.LoginAsync(
                fixture.Flow,
                It.IsAny<LoginRequest>(),
                It.IsAny<LoginExecutionOptions>(),
                fixture.HttpContext.RequestAborted))
            .Callback<AuthFlowContext,
                LoginRequest,
                LoginExecutionOptions,
                CancellationToken>(
                    (_, _, options, _) =>
                    {
                        capturedOptions = options;
                    })
            .ReturnsAsync(LoginResult.SuccessPreview());

        fixture.RedirectResolver
            .Setup(x => x.ResolveSuccess(
                fixture.Flow,
                fixture.HttpContext))
            .Returns(RedirectDecision.None());

        var result = await fixture.Sut.LoginAsync(
            fixture.HttpContext);

        result.Should().BeOfType<Ok>();

        capturedOptions.Should().NotBeNull();

        capturedOptions!.Mode.Should()
            .Be(LoginExecutionMode.Commit);

        capturedOptions.SuppressFailureAttempt.Should()
            .BeFalse();

        capturedOptions.SuppressSuccessReset.Should()
            .BeFalse();

        fixture.AuthStore.Verify(
            x => x.ConsumeAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static Fixture CreateFixture(AuthFlowContext? flow = null)
    {
        flow ??= AuthFlowTestFactory.LoginSuccess();

        var authFlow = new Mock<IAuthFlowContextAccessor>(MockBehavior.Strict);

        var internalFlow = new Mock<IUAuthInternalFlowService>(MockBehavior.Strict);

        var credentialWriter = new Mock<ICredentialResponseWriter>(MockBehavior.Strict);

        var redirectResolver = new Mock<IAuthRedirectResolver>(MockBehavior.Loose);

        var authStore = new Mock<IAuthStore>(MockBehavior.Strict);

        var identifierResolver = new Mock<ILoginIdentifierResolver>(MockBehavior.Strict);

        var clock = new Mock<IClock>(MockBehavior.Strict);

        authFlow
            .SetupGet(x => x.Current)
            .Returns(flow);

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(Now);

        var options = Options.Create(
            TestServerOptions.Default());

        var httpContext =
            new DefaultHttpContext();

        var sut = new LoginEndpointHandler(
            authFlow.Object,
            internalFlow.Object,
            credentialWriter.Object,
            redirectResolver.Object,
            authStore.Object,
            identifierResolver.Object,
            options,
            clock.Object);

        return new Fixture(
            sut,
            flow,
            internalFlow,
            credentialWriter,
            redirectResolver,
            authStore,
            identifierResolver,
            httpContext);
    }

    private static void SetLoginJson(
        HttpContext context,
        string identifier,
        string secret,
        string? previewReceipt = null)
    {
        SetJsonBody(
            context,
            new LoginRequest
            {
                Identifier = identifier,
                Secret = secret,
                Factor = CredentialType.Password,
                PreviewReceipt = previewReceipt
            });
    }

    private static void SetJsonBody(
        HttpContext context,
        object value)
    {
        var json =
            JsonSerializer.Serialize(value);

        var bytes =
            Encoding.UTF8.GetBytes(json);

        context.Request.ContentType =
            "application/json";

        context.Request.ContentLength =
            bytes.Length;

        context.Request.Body =
            new MemoryStream(bytes);
    }

    private static LoginIdentifierResolution CreateIdentifierResolution(
        TenantKey tenant,
        UserKey userKey,
        string identifier)
    {
        return new LoginIdentifierResolution
        {
            Tenant = tenant,
            UserKey = userKey,
            RawIdentifier = identifier,
            NormalizedIdentifier = identifier,
            IsVerified = true
        };
    }

    private sealed record Fixture(
        LoginEndpointHandler Sut,
        AuthFlowContext Flow,
        Mock<IUAuthInternalFlowService> InternalFlow,
        Mock<ICredentialResponseWriter> CredentialWriter,
        Mock<IAuthRedirectResolver> RedirectResolver,
        Mock<IAuthStore> AuthStore,
        Mock<ILoginIdentifierResolver> IdentifierResolver,
        DefaultHttpContext HttpContext);
}