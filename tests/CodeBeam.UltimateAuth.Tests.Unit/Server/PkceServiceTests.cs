using System.Security.Cryptography;
using System.Text;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Server.Stores;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class PkceServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private const int AuthorizationCodeLifetimeSeconds = 120;

    // ---------------------------------------------------------------------
    // Authorize
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AuthorizeAsync_WhenCodeChallengeIsMissing_ShouldReject(
        string? codeChallenge)
    {
        var fixture = CreateFixture();

        var command = CreateAuthorizeCommand(
            codeChallenge: codeChallenge!);

        var act = () =>
            fixture.Sut.AuthorizeAsync(command);

        await act.Should()
            .ThrowAsync<InvalidOperationException>();

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain")]
    [InlineData("s256")]
    [InlineData("S512")]
    public async Task AuthorizeAsync_WhenChallengeMethodIsNotExactS256_ShouldReject(
        string challengeMethod)
    {
        var fixture = CreateFixture();

        var command = CreateAuthorizeCommand(
            challengeMethod: challengeMethod);

        var act = () =>
            fixture.Sut.AuthorizeAsync(command);

        await act.Should()
            .ThrowAsync<InvalidOperationException>();

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AuthorizeAsync_ShouldStorePkceAuthorizationArtifact()
    {
        var fixture = CreateFixture();

        var device = CreateDevice();

        var command = CreateAuthorizeCommand(
            device: device,
            redirectUri: "/callback");

        AuthArtifactKey? capturedKey = null;
        PkceAuthorizationArtifact? capturedArtifact = null;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthArtifactKey, AuthArtifact, CancellationToken>(
                (key, artifact, _) =>
                {
                    capturedKey = key;
                    capturedArtifact =
                        artifact as PkceAuthorizationArtifact;
                })
            .Returns(Task.CompletedTask);

        var result =
            await fixture.Sut.AuthorizeAsync(command);

        capturedKey.Should().NotBeNull();
        capturedArtifact.Should().NotBeNull();

        capturedArtifact!.AuthorizationCode
            .Should().Be(capturedKey);

        capturedArtifact.CodeChallenge
            .Should().Be(command.CodeChallenge);

        capturedArtifact.ChallengeMethod
            .Should().Be(PkceChallengeMethod.S256);

        capturedArtifact.ExpiresAt
            .Should().Be(
                Now.AddSeconds(
                    AuthorizationCodeLifetimeSeconds));

        capturedArtifact.Context.ClientProfile
            .Should().Be(command.ClientProfile);

        capturedArtifact.Context.Tenant
            .Should().Be(command.Tenant);

        capturedArtifact.Context.RedirectUri
            .Should().Be(command.RedirectUri);

        capturedArtifact.Context.Device
            .Should().BeSameAs(device);

        result.ExpiresIn
            .Should().Be(
                AuthorizationCodeLifetimeSeconds);
    }

    [Fact]
    public async Task AuthorizeAsync_ShouldPropagateCancellationTokenToStore()
    {
        var fixture = CreateFixture();

        using var cts =
            new CancellationTokenSource();

        var command = CreateAuthorizeCommand();

        await fixture.Sut.AuthorizeAsync(
            command,
            cts.Token);

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                cts.Token),
            Times.Once);
    }

    // ---------------------------------------------------------------------
    // Complete
    // ---------------------------------------------------------------------

    [Fact]
    public async Task CompleteAsync_WhenAuthorizationArtifactDoesNotExist_ShouldReturnInvalidPkce()
    {
        var fixture = CreateFixture();

        fixture.Store
            .Setup(x => x.ConsumeAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthArtifact?)null);

        var auth = AuthFlowTestFactory.New();
        var request = CreateCompleteRequest();

        var result =
            await fixture.Sut.CompleteAsync(
                auth,
                request);

        result.InvalidPkce.Should().BeTrue();

        fixture.Validator.Verify(
            x => x.Validate(
                It.IsAny<PkceAuthorizationArtifact>(),
                It.IsAny<string>(),
                It.IsAny<PkceContextSnapshot>(),
                It.IsAny<DateTimeOffset>()),
            Times.Never);

        fixture.Flow.Verify(
            x => x.LoginAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<AuthExecutionContext>(),
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_ShouldConsumeAuthorizationCodeUsingRequestCode()
    {
        var fixture = CreateFixture();

        const string authorizationCode =
            "authorization-code";

        fixture.Store
            .Setup(x => x.ConsumeAsync(
                It.Is<AuthArtifactKey>(
                    key =>
                        key.Value == authorizationCode),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthArtifact?)null);

        var request = CreateCompleteRequest(
            authorizationCode: authorizationCode);

        await fixture.Sut.CompleteAsync(
            AuthFlowTestFactory.New(),
            request);

        fixture.Store.Verify(
            x => x.ConsumeAsync(
                It.Is<AuthArtifactKey>(
                    key =>
                        key.Value == authorizationCode),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_WhenPkceValidationFails_ShouldNotAttemptLogin()
    {
        var fixture = CreateFixture();

        var artifact = CreatePkceArtifact();

        SetupConsumedArtifact(
            fixture,
            artifact);

        fixture.Validator
            .Setup(x => x.Validate(
                artifact,
                It.IsAny<string>(),
                It.IsAny<PkceContextSnapshot>(),
                Now))
            .Returns(CreateFailedValidation());

        var result =
            await fixture.Sut.CompleteAsync(
                AuthFlowTestFactory.New(),
                CreateCompleteRequest());

        result.IsSuccess.Should().BeFalse();
        result.FailureReason
            .Should().Be(
                AuthFailureReason.InvalidCredentials);

        fixture.Flow.Verify(
            x => x.LoginAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<AuthExecutionContext>(),
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_WhenValidationSucceeds_ShouldPassCredentialsToLogin()
    {
        var fixture = CreateFixture();

        var artifact = CreatePkceArtifact();

        SetupSuccessfulValidation(
            fixture,
            artifact);

        LoginRequest? captured = null;

        fixture.Flow
            .Setup(x => x.LoginAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<AuthExecutionContext>(),
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<
                AuthFlowContext,
                AuthExecutionContext,
                LoginRequest,
                CancellationToken>(
                (_, _, request, _) =>
                {
                    captured = request;
                })
            .ReturnsAsync(CreateSuccessfulLoginResult());

        var request = CreateCompleteRequest(
            identifier: "alice@example.com",
            secret: "correct-password");

        await fixture.Sut.CompleteAsync(
            AuthFlowTestFactory.New(),
            request);

        captured.Should().NotBeNull();

        captured!.Identifier
            .Should().Be("alice@example.com");

        captured.Secret
            .Should().Be("correct-password");
    }

    [Fact]
    public async Task CompleteAsync_WhenValidationSucceeds_ShouldUseArtifactExecutionContext()
    {
        var fixture = CreateFixture();

        var device = CreateDevice();

        var artifact = CreatePkceArtifact(
            clientProfile:
                UAuthClientProfile.BlazorWasm,
            device: device);

        SetupSuccessfulValidation(
            fixture,
            artifact);

        AuthExecutionContext? captured = null;

        fixture.Flow
            .Setup(x => x.LoginAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<AuthExecutionContext>(),
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<
                AuthFlowContext,
                AuthExecutionContext,
                LoginRequest,
                CancellationToken>(
                (_, execution, _, _) =>
                {
                    captured = execution;
                })
            .ReturnsAsync(CreateSuccessfulLoginResult());

        await fixture.Sut.CompleteAsync(
            AuthFlowTestFactory.New(),
            CreateCompleteRequest());

        captured.Should().NotBeNull();

        captured!.EffectiveClientProfile
            .Should().Be(
                UAuthClientProfile.BlazorWasm);

        captured.Device
            .Should().BeSameAs(device);
    }

    [Fact]
    public async Task CompleteAsync_ShouldPropagateCancellationTokenToConsumeAndLogin()
    {
        var fixture = CreateFixture();

        var artifact = CreatePkceArtifact();

        SetupSuccessfulValidation(
            fixture,
            artifact);

        fixture.Flow
            .Setup(x => x.LoginAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<AuthExecutionContext>(),
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessfulLoginResult());

        using var cts =
            new CancellationTokenSource();

        await fixture.Sut.CompleteAsync(
            AuthFlowTestFactory.New(),
            CreateCompleteRequest(),
            cts.Token);

        fixture.Store.Verify(
            x => x.ConsumeAsync(
                It.IsAny<AuthArtifactKey>(),
                cts.Token),
            Times.Once);

        fixture.Flow.Verify(
            x => x.LoginAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<AuthExecutionContext>(),
                It.IsAny<LoginRequest>(),
                cts.Token),
            Times.Once);
    }

    // ---------------------------------------------------------------------
    // Refresh
    // ---------------------------------------------------------------------

    [Fact]
    public async Task RefreshAsync_WhenOldAuthorizationCodeExists_ShouldConsumeIt()
    {
        var fixture = CreateFixture();

        var hub = CreateHubArtifact(
            authorizationCode: "old-code");

        await fixture.Sut.RefreshAsync(hub);

        fixture.Store.Verify(
            x => x.ConsumeAsync(
                It.Is<AuthArtifactKey>(
                    key =>
                        key.Value == "old-code"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RefreshAsync_WhenOldAuthorizationCodeDoesNotExist_ShouldNotConsume(
        string? oldCode)
    {
        var fixture = CreateFixture();

        var hub = CreateHubArtifact(
            authorizationCode: oldCode);

        await fixture.Sut.RefreshAsync(hub);

        fixture.Store.Verify(
            x => x.ConsumeAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_ShouldCreateNewPkceArtifactWithHubContext()
    {
        var fixture = CreateFixture();

        var device = CreateDevice();

        var hub = CreateHubArtifact(
            authorizationCode: "old-code",
            device: device,
            returnUrl: "/orders");

        PkceAuthorizationArtifact? captured = null;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthArtifactKey, AuthArtifact, CancellationToken>(
                (_, artifact, _) =>
                {
                    captured =
                        artifact as PkceAuthorizationArtifact;
                })
            .Returns(Task.CompletedTask);

        var result =
            await fixture.Sut.RefreshAsync(hub);

        captured.Should().NotBeNull();

        captured!.AuthorizationCode.Value
            .Should().Be(
                result.AuthorizationCode);

        captured.ChallengeMethod
            .Should().Be(
                PkceChallengeMethod.S256);

        captured.ExpiresAt
            .Should().Be(
                Now.AddSeconds(
                    AuthorizationCodeLifetimeSeconds));

        captured.Context.ClientProfile
            .Should().Be(hub.ClientProfile);

        captured.Context.Tenant
            .Should().Be(hub.Tenant);

        captured.Context.RedirectUri
            .Should().Be(hub.ReturnUrl);

        captured.Context.Device
            .Should().BeSameAs(device);
    }

    [Fact]
    public async Task RefreshAsync_ReturnedVerifier_ShouldMatchStoredS256Challenge()
    {
        var fixture = CreateFixture();

        var hub = CreateHubArtifact();

        PkceAuthorizationArtifact? captured = null;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthArtifactKey, AuthArtifact, CancellationToken>(
                (_, artifact, _) =>
                {
                    captured =
                        artifact as PkceAuthorizationArtifact;
                })
            .Returns(Task.CompletedTask);

        var result =
            await fixture.Sut.RefreshAsync(hub);

        captured.Should().NotBeNull();

        CreateS256Challenge(result.CodeVerifier)
            .Should().Be(captured!.CodeChallenge);
    }

    [Fact]
    public async Task RefreshAsync_ShouldPropagateCancellationTokenToStoreOperations()
    {
        var fixture = CreateFixture();

        var hub = CreateHubArtifact(
            authorizationCode: "old-code");

        using var cts =
            new CancellationTokenSource();

        await fixture.Sut.RefreshAsync(
            hub,
            cts.Token);

        fixture.Store.Verify(
            x => x.ConsumeAsync(
                It.Is<AuthArtifactKey>(
                    key => key.Value == "old-code"),
                cts.Token),
            Times.Once);

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_WhenLoginFails_ShouldMapLoginFailure()
    {
        var fixture = CreateFixture();

        var artifact = CreatePkceArtifact();

        SetupSuccessfulValidation(
            fixture,
            artifact);

        var loginResult =
            LoginResult.Failed(
                AuthFailureReason.InvalidCredentials);

        fixture.Flow
            .Setup(x => x.LoginAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<AuthExecutionContext>(),
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginResult);

        var result =
            await fixture.Sut.CompleteAsync(
                AuthFlowTestFactory.New(),
                CreateCompleteRequest());

        result.IsSuccess.Should().BeFalse();

        result.FailureReason.Should().Be(
            AuthFailureReason.InvalidCredentials);

        result.LoginResult.Should().BeSameAs(loginResult);
    }

    [Fact]
    public async Task CompleteAsync_WhenLoginSucceeds_ShouldReturnLoginResult()
    {
        var fixture = CreateFixture();

        var artifact = CreatePkceArtifact();

        SetupSuccessfulValidation(
            fixture,
            artifact);

        var loginResult =
            LoginResult.SuccessPreview();

        fixture.Flow
            .Setup(x => x.LoginAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<AuthExecutionContext>(),
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginResult);

        var result =
            await fixture.Sut.CompleteAsync(
                AuthFlowTestFactory.New(),
                CreateCompleteRequest());

        result.IsSuccess.Should().BeTrue();
        result.FailureReason.Should().BeNull();
        result.LoginResult.Should().BeSameAs(loginResult);
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static Fixture CreateFixture()
    {
        var store = new Mock<IAuthStore>();
        var validator =
            new Mock<IPkceAuthorizationValidator>();
        var flow =
            new Mock<IUAuthFlowService>();
        var clock =
            new Mock<IClock>();

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(Now);

        store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new UAuthServerOptions();

        options.Pkce.AuthorizationCodeLifetimeSeconds =
            AuthorizationCodeLifetimeSeconds;

        var sut = new PkceService(
            store.Object,
            validator.Object,
            flow.Object,
            clock.Object,
            Options.Create(options));

        return new Fixture(
            sut,
            store,
            validator,
            flow);
    }

    private static void SetupConsumedArtifact(
        Fixture fixture,
        PkceAuthorizationArtifact artifact)
    {
        fixture.Store
            .Setup(x => x.ConsumeAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);
    }

    private static void SetupSuccessfulValidation(
        Fixture fixture,
        PkceAuthorizationArtifact artifact)
    {
        SetupConsumedArtifact(
            fixture,
            artifact);

        fixture.Validator
            .Setup(x => x.Validate(
                artifact,
                It.IsAny<string>(),
                It.IsAny<PkceContextSnapshot>(),
                Now))
            .Returns(CreateSuccessfulValidation());
    }

    private static string CreateS256Challenge(
        string verifier)
    {
        var bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(verifier));

        return Convert
            .ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed record Fixture(
        PkceService Sut,
        Mock<IAuthStore> Store,
        Mock<IPkceAuthorizationValidator> Validator,
        Mock<IUAuthFlowService> Flow);

    private static PkceAuthorizeCommand CreateAuthorizeCommand(
    string codeChallenge = "valid-code-challenge",
    string challengeMethod = "S256",
    DeviceContext? device = null,
    string? redirectUri = "/callback",
    UAuthClientProfile clientProfile = UAuthClientProfile.BlazorWasm,
    TenantKey? tenant = null)
    {
        return new PkceAuthorizeCommand
        {
            CodeChallenge = codeChallenge,
            ChallengeMethod = challengeMethod,
            Device = device ?? DeviceContext.Anonymous(),
            RedirectUri = redirectUri,
            ClientProfile = clientProfile,
            Tenant = tenant ?? TenantKey.Single
        };
    }

    private static PkceCompleteRequest CreateCompleteRequest(
        string authorizationCode = "authorization-code",
        string codeVerifier = "verifier",
        string identifier = "alice@example.com",
        string secret = "correct-password",
        string? returnUrl = "/dashboard",
        string? hubSessionId = null)
    {
        return new PkceCompleteRequest
        {
            AuthorizationCode = authorizationCode,
            CodeVerifier = codeVerifier,
            Identifier = identifier,
            Secret = secret,
            ReturnUrl = returnUrl,
            HubSessionId = hubSessionId
        };
    }

    private static PkceAuthorizationArtifact CreatePkceArtifact(
        string authorizationCode = "authorization-code",
        string codeChallenge = "code-challenge",
        PkceChallengeMethod challengeMethod = PkceChallengeMethod.S256,
        DateTimeOffset? expiresAt = null,
        UAuthClientProfile clientProfile = UAuthClientProfile.BlazorWasm,
        TenantKey? tenant = null,
        string? redirectUri = "/callback",
        DeviceContext? device = null)
    {
        var context = new PkceContextSnapshot(
            clientProfile,
            tenant ?? TenantKey.Single,
            redirectUri,
            device ?? DeviceContext.Anonymous());

        return new PkceAuthorizationArtifact(
            new AuthArtifactKey(authorizationCode),
            codeChallenge,
            challengeMethod,
            expiresAt ?? Now.AddMinutes(2),
            context);
    }

    private static HubFlowArtifact CreateHubArtifact(
        string? authorizationCode = null,
        UAuthClientProfile clientProfile = UAuthClientProfile.BlazorWasm,
        TenantKey? tenant = null,
        DeviceContext? device = null,
        string? returnUrl = "/callback")
    {
        var payload = new HubFlowPayload();

        if (authorizationCode is not null)
            payload.Set("authorization_code", authorizationCode);

        return new HubFlowArtifact(
            HubSessionId.New(),
            HubFlowType.Login,
            clientProfile,
            tenant ?? TenantKey.Single,
            device ?? DeviceContext.Anonymous(),
            returnUrl,
            payload,
            Now.AddMinutes(5));
    }

    private static DeviceContext CreateDevice()
    {
        // DeviceId factory API'sini henüz görmediğimiz için testin bu
        // aşamasında anonymous olmayan sahte DeviceId üretmiyoruz.
        //
        // PkceService açısından önemli invariant aynı DeviceContext
        // instance'ının snapshot/execution'a taşınmasıdır.
        return DeviceContext.Create(DeviceId.Create("test-device-id123456789012345678901234567890"), "Desktop", "Web", "Windows", "Edge", "127.0.0.1");
    }

    private static PkceValidationResult CreateSuccessfulValidation()
    {
        return PkceValidationResult.Ok();
    }

    private static PkceValidationResult CreateFailedValidation()
    {
        return PkceValidationResult.Fail(PkceValidationFailureReason.InvalidVerifier);
    }

    private static LoginResult CreateSuccessfulLoginResult()
    {
        return LoginResult.SuccessPreview();
    }
}
