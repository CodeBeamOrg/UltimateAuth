using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthLoginFormSubmissionTests : BunitContext
{
    private readonly Mock<IDeviceIdProvider> _deviceIdProvider = new();
    private readonly Mock<IUAuthClient> _client = new();
    private readonly Mock<IFlowClient> _flows = new();
    private readonly Mock<IHubCredentialResolver> _credentialResolver = new();
    private readonly Mock<IHubFlowReader> _hubFlowReader = new();
    private readonly Mock<IHubCapabilities> _hubCapabilities = new();

    public UAuthLoginFormSubmissionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        _client
            .SetupGet(x => x.Flows)
            .Returns(_flows.Object);

        _hubCapabilities
            .SetupGet(x => x.SupportsPkce)
            .Returns(true);

        Services.AddSingleton(_deviceIdProvider.Object);
        Services.AddSingleton(_client.Object);
        Services.AddSingleton(_credentialResolver.Object);
        Services.AddSingleton(_hubFlowReader.Object);
        Services.AddSingleton(_hubCapabilities.Object);

        Services.AddSingleton(
            Options.Create(new UAuthClientOptions()));
    }

    // -----------------------------------------------------------------
    // Password validation
    // -----------------------------------------------------------------

    [Theory]
    [InlineData(null, "secret")]
    [InlineData("", "secret")]
    [InlineData(" ", "secret")]
    [InlineData("alice", null)]
    [InlineData("alice", "")]
    [InlineData("alice", " ")]
    public async Task PasswordSubmit_WhenCredentialsMissing_ThrowsValidationException(string? identifier, string? secret)
    {
        var cut = RenderPasswordForm(
            identifier,
            secret,
            UAuthSubmitMode.TryOnly);

        var act = () => cut.Instance.SubmitAsync();

        await act.Should().ThrowAsync<UAuthValidationException>();
    }

    // -----------------------------------------------------------------
    // Password / TryOnly
    // -----------------------------------------------------------------

    [Fact]
    public async Task Password_TryOnly_SendsExpectedLoginRequest()
    {
        LoginRequest? captured = null;

        var result = new TryLoginResult
        {
            Success = true
        };

        _client
            .Setup(x => x.Flows.TryLoginAsync(
                It.IsAny<LoginRequest>(),
                UAuthSubmitMode.TryOnly,
                It.IsAny<string?>()))
            .Callback<LoginRequest, UAuthSubmitMode, string?>(
                (request, _, _) => captured = request)
            .ReturnsAsync(result);

        var cut = RenderPasswordForm(
            "alice@example.com",
            "secret-123",
            UAuthSubmitMode.TryOnly);

        await cut.Instance.SubmitAsync();

        captured.Should().NotBeNull();
        captured!.Identifier.Should().Be("alice@example.com");
        captured.Secret.Should().Be("secret-123");
    }

    [Fact]
    public async Task Password_TryOnly_UsesTryOnlyMode()
    {
        var result = CreateTryLoginResult();

        _client
            .Setup(x => x.Flows.TryLoginAsync(
                It.IsAny<LoginRequest>(),
                UAuthSubmitMode.TryOnly,
                It.IsAny<string?>()))
            .ReturnsAsync(result);

        var cut = RenderPasswordForm(
            "alice",
            "secret",
            UAuthSubmitMode.TryOnly);

        await cut.Instance.SubmitAsync();

        _client.Verify(
            x => x.Flows.TryLoginAsync(
                It.Is<LoginRequest>(r =>
                    r.Identifier == "alice" &&
                    r.Secret == "secret"),
                UAuthSubmitMode.TryOnly,
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task Password_TryOnly_EmitsTryResult()
    {
        var result = new TryLoginResult
        {
            Success = true
        };

        _client
            .Setup(x => x.Flows.TryLoginAsync(
                It.IsAny<LoginRequest>(),
                UAuthSubmitMode.TryOnly,
                It.IsAny<string?>()))
            .ReturnsAsync(result);

        IUAuthTryResult? emitted = null;

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.Identifier, "alice")
            .Add(x => x.Secret, "secret")
            .Add(x => x.SubmitMode, UAuthSubmitMode.TryOnly)
            .Add(x => x.OnTryResult,
                r => emitted = r));

        await cut.Instance.SubmitAsync();

        emitted.Should().BeSameAs(result);
    }

    // -----------------------------------------------------------------
    // Password / TryAndCommit
    // -----------------------------------------------------------------

    [Fact]
    public async Task Password_TryAndCommit_SendsEffectiveReturnUrl()
    {
        string? capturedReturnUrl = null;

        var result = new TryLoginResult
        {
            Success = true
        };

        _client
            .Setup(x => x.Flows.TryLoginAsync(
                It.IsAny<LoginRequest>(),
                UAuthSubmitMode.TryAndCommit,
                It.IsAny<string?>()))
            .Callback<LoginRequest, UAuthSubmitMode, string?>(
                (_, _, returnUrl) =>
                    capturedReturnUrl = returnUrl)
            .ReturnsAsync(result);

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.Identifier, "alice")
            .Add(x => x.Secret, "secret")
            .Add(x => x.ReturnUrl, "/dashboard")
            .Add(x => x.SubmitMode, UAuthSubmitMode.TryAndCommit));

        await cut.Instance.SubmitAsync();

        capturedReturnUrl.Should().Be("/dashboard");
    }

    [Fact]
    public async Task Password_TryAndCommit_EmitsTryResult()
    {
        var result = CreateTryLoginResult();

        _client
            .Setup(x => x.Flows.TryLoginAsync(
                It.IsAny<LoginRequest>(),
                UAuthSubmitMode.TryAndCommit,
                It.IsAny<string?>()))
            .ReturnsAsync(result);

        IUAuthTryResult? emitted = null;

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.Identifier, "alice")
            .Add(x => x.Secret, "secret")
            .Add(x => x.ReturnUrl, "/home")
            .Add(x => x.SubmitMode, UAuthSubmitMode.TryAndCommit)
            .Add(x => x.OnTryResult,
                r => emitted = r));

        await cut.Instance.SubmitAsync();

        emitted.Should().BeSameAs(result);
    }

    // -----------------------------------------------------------------
    // Password / DirectCommit
    // -----------------------------------------------------------------

    [Fact]
    public async Task Password_DirectCommit_UsesNativeFormSubmission()
    {
        var cut = RenderPasswordForm(
            "alice",
            "secret",
            UAuthSubmitMode.DirectCommit);

        await cut.Instance.SubmitAsync();

        JSInterop.VerifyInvoke("uauth.submitForm");
    }

    [Fact]
    public async Task Password_DirectCommit_DoesNotCallTryLogin()
    {
        var cut = RenderPasswordForm(
            "alice",
            "secret",
            UAuthSubmitMode.DirectCommit);

        await cut.Instance.SubmitAsync();

        _client.Verify(
            x => x.Flows.TryLoginAsync(
                It.IsAny<LoginRequest>(),
                It.IsAny<UAuthSubmitMode>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task Password_DirectCommit_DoesNotEmitTryResult()
    {
        var callbackCount = 0;

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.Identifier, "alice")
            .Add(x => x.Secret, "secret")
            .Add(x => x.SubmitMode, UAuthSubmitMode.DirectCommit)
            .Add(x => x.OnTryResult,
                _ => callbackCount++));

        await cut.Instance.SubmitAsync();

        callbackCount.Should().Be(0);
    }

    [Fact]
    public async Task Password_DirectCommit_DoesNotCallFlowClient()
    {
        var cut = RenderPasswordForm(
            "alice",
            "secret",
            UAuthSubmitMode.DirectCommit);

        await cut.Instance.SubmitAsync();

        _client.Verify(
            x => x.Flows.TryLoginAsync(
                It.IsAny<LoginRequest>(),
                It.IsAny<UAuthSubmitMode>(),
                It.IsAny<string?>()),
            Times.Never);

        _client.Verify(
            x => x.Flows.LoginAsync(
                It.IsAny<LoginRequest>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    // -----------------------------------------------------------------
    // PKCE validation
    // -----------------------------------------------------------------

    [Fact]
    public void Pkce_WhenHubDoesNotSupportPkce_Throws()
    {
        _hubCapabilities
            .SetupGet(x => x.SupportsPkce)
            .Returns(false);

        var hubId = HubSessionId.New();

        var act = () => Render<UAuthLoginForm>(p => p
            .Add(x => x.LoginType, UAuthLoginType.Pkce)
            .Add(x => x.HubSessionId, hubId));

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*PKCE login requires UAuthHub*");
    }

    [Fact]
    public async Task Pkce_WhenCredentialsMissing_Throws()
    {
        var hubId = HubSessionId.New();

        _credentialResolver
            .Setup(x => x.ResolveAsync(
                hubId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HubCredentials?)null);

        _hubFlowReader
            .Setup(x => x.GetStateAsync(
                hubId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateHubState(hubId));

        var cut = RenderPkceForm(
            hubId,
            "alice",
            "secret",
            UAuthSubmitMode.TryOnly);

        var act = () => cut.Instance.SubmitAsync();

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Missing PKCE credentials.");
    }

    // -----------------------------------------------------------------
    // PKCE / TryOnly
    // -----------------------------------------------------------------

    [Fact]
    public async Task Pkce_TryOnly_BuildsExpectedRequest()
    {
        var hubId = HubSessionId.New();

        SetupPkce(
            hubId,
            authorizationCode: "auth-code",
            codeVerifier: "verifier",
            returnUrl: "/dashboard");

        PkceCompleteRequest? captured = null;

        var result = new TryPkceLoginResult
        {
            Success = true
        };

        _client
            .Setup(x => x.Flows.TryCompletePkceLoginAsync(
                It.IsAny<PkceCompleteRequest>(),
                UAuthSubmitMode.TryOnly))
            .Callback<PkceCompleteRequest, UAuthSubmitMode>(
                (request, _) => captured = request)
            .ReturnsAsync(result);

        var cut = RenderPkceForm(
            hubId,
            "alice@example.com",
            "secret-123",
            UAuthSubmitMode.TryOnly);

        await cut.Instance.SubmitAsync();

        captured.Should().NotBeNull();

        captured!.Identifier.Should().Be("alice@example.com");
        captured.Secret.Should().Be("secret-123");

        captured.AuthorizationCode.Should().Be("auth-code");
        captured.CodeVerifier.Should().Be("verifier");

        captured.ReturnUrl.Should().Be("/dashboard");
        captured.HubSessionId.Should().Be(hubId.Value);
    }

    [Fact]
    public async Task Pkce_TryOnly_EmitsTryResult()
    {
        var hubId = HubSessionId.New();

        SetupPkce(hubId);

        var result = CreateTryPkceLoginResult();

        _client
            .Setup(x => x.Flows.TryCompletePkceLoginAsync(
                It.IsAny<PkceCompleteRequest>(),
                UAuthSubmitMode.TryOnly))
            .ReturnsAsync(result);

        IUAuthTryResult? emitted = null;

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.LoginType, UAuthLoginType.Pkce)
            .Add(x => x.HubSessionId, hubId)
            .Add(x => x.Identifier, "alice")
            .Add(x => x.Secret, "secret")
            .Add(x => x.SubmitMode, UAuthSubmitMode.TryOnly)
            .Add(x => x.OnTryResult,
                r => emitted = r));

        await cut.Instance.SubmitAsync();

        emitted.Should().BeSameAs(result);
    }

    // -----------------------------------------------------------------
    // PKCE / TryAndCommit
    // -----------------------------------------------------------------

    [Fact]
    public async Task Pkce_TryAndCommit_UsesTryAndCommitMode()
    {
        var hubId = HubSessionId.New();

        SetupPkce(hubId);

        var result = new TryPkceLoginResult
        {
            Success = true
        };

        _client
            .Setup(x => x.Flows.TryCompletePkceLoginAsync(
                It.IsAny<PkceCompleteRequest>(),
                UAuthSubmitMode.TryAndCommit))
            .ReturnsAsync(result);

        var cut = RenderPkceForm(
            hubId,
            "alice",
            "secret",
            UAuthSubmitMode.TryAndCommit);

        await cut.Instance.SubmitAsync();

        _client.Verify(
            x => x.Flows.TryCompletePkceLoginAsync(
                It.Is<PkceCompleteRequest>(r =>
                    r.Identifier == "alice" &&
                    r.Secret == "secret" &&
                    r.HubSessionId == hubId.Value),
                UAuthSubmitMode.TryAndCommit),
            Times.Once);
    }

    // -----------------------------------------------------------------
    // PKCE / DirectCommit
    // -----------------------------------------------------------------

    [Fact]
    public async Task Pkce_DirectCommit_CompletesPkceDirectly()
    {
        var hubId = HubSessionId.New();

        SetupPkce(hubId);

        _flows
            .Setup(x => x.CompletePkceLoginAsync(
                It.IsAny<PkceCompleteRequest>()))
            .Returns(Task.CompletedTask);

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.LoginType, UAuthLoginType.Pkce)
            .Add(x => x.HubSessionId, hubId)
            .Add(x => x.Identifier, "alice")
            .Add(x => x.Secret, "secret")
            .Add(x => x.ReturnUrl, "/home")
            .Add(x => x.SubmitMode, UAuthSubmitMode.DirectCommit));

        await cut.Instance.SubmitAsync();

        _flows.Verify(
            x => x.CompletePkceLoginAsync(
                It.Is<PkceCompleteRequest>(r =>
                    r.Identifier == "alice" &&
                    r.Secret == "secret" &&
                    r.ReturnUrl == "/home")),
            Times.Once);
    }

    [Fact]
    public async Task Pkce_DirectCommit_DoesNotUseNativeFormSubmission()
    {
        var hubId = HubSessionId.New();

        SetupPkce(hubId);

        _flows
            .Setup(x => x.CompletePkceLoginAsync(It.IsAny<PkceCompleteRequest>()))
            .Returns(Task.CompletedTask);

        var cut = RenderPkceForm(
            hubId,
            "alice",
            "secret",
            UAuthSubmitMode.DirectCommit);

        await cut.Instance.SubmitAsync();

        JSInterop.Invocations
            .Should()
            .NotContain(x => x.Identifier == "uauth.submitForm");
    }

    [Fact]
    public async Task Pkce_DirectCommit_DoesNotEmitTryResult()
    {
        var hubId = HubSessionId.New();

        SetupPkce(hubId);

        _flows
            .Setup(x => x.CompletePkceLoginAsync(It.IsAny<PkceCompleteRequest>()))
            .Returns(Task.CompletedTask);

        var callbackCount = 0;

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.LoginType, UAuthLoginType.Pkce)
            .Add(x => x.HubSessionId, hubId)
            .Add(x => x.Identifier, "alice")
            .Add(x => x.Secret, "secret")
            .Add(x => x.SubmitMode, UAuthSubmitMode.DirectCommit)
            .Add(x => x.OnTryResult,
                _ => callbackCount++));

        await cut.Instance.SubmitAsync();

        callbackCount.Should().Be(0);
    }

    // -----------------------------------------------------------------
    // Hub resolution
    // -----------------------------------------------------------------

    [Fact]
    public async Task Pkce_HubSessionIdFromQuery_IsUsed()
    {
        var hubId = HubSessionId.New();

        SetupPkce(hubId);

        var nav = Services
            .GetRequiredService<NavigationManager>();

        nav.NavigateTo(
            $"/login?{UAuthConstants.Query.Hub}={Uri.EscapeDataString(hubId.Value)}");

        var result = CreateTryPkceLoginResult();

        _client
            .Setup(x => x.Flows.TryCompletePkceLoginAsync(
                It.IsAny<PkceCompleteRequest>(),
                UAuthSubmitMode.TryOnly))
            .ReturnsAsync(result);

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.LoginType, UAuthLoginType.Pkce)
            .Add(x => x.Identifier, "alice")
            .Add(x => x.Secret, "secret")
            .Add(x => x.SubmitMode, UAuthSubmitMode.TryOnly));

        await cut.Instance.SubmitAsync();

        _credentialResolver.Verify(
            x => x.ResolveAsync(
                hubId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    private IRenderedComponent<UAuthLoginForm> RenderPasswordForm(
        string? identifier,
        string? secret,
        UAuthSubmitMode mode)
    {
        return Render<UAuthLoginForm>(p => p
            .Add(x => x.Identifier, identifier)
            .Add(x => x.Secret, secret)
            .Add(x => x.LoginType, UAuthLoginType.Password)
            .Add(x => x.SubmitMode, mode));
    }

    private IRenderedComponent<UAuthLoginForm> RenderPkceForm(
        HubSessionId hubId,
        string identifier,
        string secret,
        UAuthSubmitMode mode)
    {
        return Render<UAuthLoginForm>(p => p
            .Add(x => x.Identifier, identifier)
            .Add(x => x.Secret, secret)
            .Add(x => x.LoginType, UAuthLoginType.Pkce)
            .Add(x => x.HubSessionId, hubId)
            .Add(x => x.SubmitMode, mode));
    }

    private void SetupPkce(
        HubSessionId hubId,
        string authorizationCode = "authorization-code",
        string codeVerifier = "code-verifier",
        string returnUrl = "/home")
    {
        var credentials = new HubCredentials
        {
            AuthorizationCode = authorizationCode,
            CodeVerifier = codeVerifier
        };

        _credentialResolver
            .Setup(x => x.ResolveAsync(
                hubId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);

        _hubFlowReader
            .Setup(x => x.GetStateAsync(
                hubId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                CreateHubState(
                    hubId,
                    returnUrl));
    }

    private static HubFlowState CreateHubState(HubSessionId hubId, string returnUrl = "/home")
    {
        return new HubFlowState
        {
            HubSessionId = hubId,
            Exists = true,
            IsActive = true,
            IsExpired = false,
            IsCompleted = false,
            ReturnUrl = returnUrl
        };
    }

    private static TryLoginResult CreateTryLoginResult()
    {
        return new TryLoginResult
        {
            Success = true
        };
    }

    private static TryPkceLoginResult CreateTryPkceLoginResult()
    {
        return new TryPkceLoginResult
        {
            Success = true
        };
    }
}
