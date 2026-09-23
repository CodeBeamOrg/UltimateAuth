using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Device;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthLoginFormTests : BunitContext
{
    private readonly Mock<IDeviceIdProvider> _deviceIdProvider = new();
    private readonly Mock<IUAuthClient> _uauthClient = new();
    private readonly Mock<IHubCredentialResolver> _hubCredentialResolver = new();
    private readonly Mock<IHubFlowReader> _hubFlowReader = new();
    private readonly Mock<IHubCapabilities> _hubCapabilities = new();

    public UAuthLoginFormTests()
    {
        var options = new UAuthClientOptions();

        Services.AddSingleton<IOptions<UAuthClientOptions>>(
            Options.Create(options));

        Services.AddSingleton(_deviceIdProvider.Object);
        Services.AddSingleton(_uauthClient.Object);
        Services.AddSingleton(_hubCredentialResolver.Object);
        Services.AddSingleton(_hubFlowReader.Object);
        Services.AddSingleton(_hubCapabilities.Object);

        _deviceIdProvider
            .Setup(x => x.GetOrCreateAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(DeviceId));

        _hubCapabilities
            .SetupGet(x => x.SupportsPkce)
            .Returns(true);
    }

    // -----------------------------------------------------------------
    // Basic rendering
    // -----------------------------------------------------------------

    [Fact]
    public void Render_RendersForm()
    {
        var cut = RenderLoginForm();

        cut.FindAll("form").Should().ContainSingle();
    }

    [Fact]
    public void Render_RendersIdentifierHiddenInput()
    {
        var cut = RenderLoginForm(
            identifier: "alice@example.com");

        var input = cut.Find("input[name='Identifier']");

        input.GetAttribute("value")
            .Should()
            .Be("alice@example.com");
    }

    [Fact]
    public void Render_RendersSecretHiddenInput()
    {
        var cut = RenderLoginForm(
            secret: "super-secret");

        var input = cut.Find("input[name='Secret']");

        input.GetAttribute("value")
            .Should()
            .Be("super-secret");
    }

    [Fact]
    public void Render_SecretInput_DisablesAutocomplete()
    {
        var cut = RenderLoginForm(
            secret: "secret");

        var input = cut.Find("input[name='Secret']");

        input.GetAttribute("autocomplete")
            .Should()
            .Be("off");
    }

    // -----------------------------------------------------------------
    // UltimateAuth form contract
    // -----------------------------------------------------------------

    [Fact]
    public void Render_UsesClientProfileFormContractName()
    {
        var cut = RenderLoginForm();

        cut.FindAll(
                $"input[name='{UAuthConstants.Form.ClientProfile}']")
            .Should()
            .ContainSingle();
    }

    [Fact]
    public void Render_UsesDeviceFormContractName()
    {
        var cut = RenderLoginForm();

        cut.FindAll(
                $"input[name='{UAuthConstants.Form.Device}']")
            .Should()
            .ContainSingle();
    }

    [Fact]
    public void DirectCommit_RendersReturnUrlUsingFormContractName()
    {
        var cut = RenderLoginForm(
            returnUrl: "/home",
            submitMode: UAuthSubmitMode.DirectCommit);

        var input = cut.Find(
            $"input[name='{UAuthConstants.Form.ReturnUrl}']");

        input.GetAttribute("value")
            .Should()
            .Be("/home");
    }

    [Theory]
    [InlineData(UAuthSubmitMode.TryOnly)]
    [InlineData(UAuthSubmitMode.TryAndCommit)]
    public void NonDirectCommit_DoesNotRenderReturnUrlFormField(
        UAuthSubmitMode submitMode)
    {
        var cut = RenderLoginForm(
            returnUrl: "/home",
            submitMode: submitMode);

        cut.FindAll(
                $"input[name='{UAuthConstants.Form.ReturnUrl}']")
            .Should()
            .BeEmpty();
    }

    // -----------------------------------------------------------------
    // Password / PKCE markup
    // -----------------------------------------------------------------

    [Fact]
    public void PasswordLogin_DoesNotRenderAuthorizationCode()
    {
        var cut = RenderLoginForm(
            loginType: UAuthLoginType.Password);

        cut.FindAll(
                "input[name='authorization_code']")
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void PasswordLogin_DoesNotRenderCodeVerifier()
    {
        var cut = RenderLoginForm(
            loginType: UAuthLoginType.Password);

        cut.FindAll(
                "input[name='code_verifier']")
            .Should()
            .BeEmpty();
    }

    // -----------------------------------------------------------------
    // Enter-key behavior
    // -----------------------------------------------------------------

    [Fact]
    public void AllowEnterKeyToSubmitTrue_RendersHiddenSubmitButton()
    {
        var cut = RenderLoginForm(
            allowEnterKeyToSubmit: true);

        var buttons = cut.FindAll(
            "button[type='submit']");

        buttons.Should().ContainSingle();

        buttons[0]
            .HasAttribute("hidden")
            .Should()
            .BeTrue();
    }

    [Fact]
    public void AllowEnterKeyToSubmitFalse_DoesNotRenderSubmitButton()
    {
        var cut = RenderLoginForm(
            allowEnterKeyToSubmit: false);

        cut.FindAll(
                "button[type='submit']")
            .Should()
            .BeEmpty();
    }

    // -----------------------------------------------------------------
    // Child content
    // -----------------------------------------------------------------

    [Fact]
    public void Render_RendersChildContent()
    {
        var cut = RenderLoginForm(
            childContent: builder =>
            {
                builder.AddMarkupContent(
                    0,
                    "<span id=\"login-content\">Login UI</span>");
            });

        cut.Find("#login-content")
            .TextContent
            .Should()
            .Be("Login UI");
    }

    // -----------------------------------------------------------------
    // Endpoint
    // -----------------------------------------------------------------

    [Fact]
    public void ExplicitEndpoint_IsUsedAsFormAction()
    {
        var cut = RenderLoginForm(
            endpoint: "/custom-login");

        var form = cut.Find("form");

        form.GetAttribute("action")
            .Should()
            .Contain("/custom-login");
    }

    [Fact]
    public void ReturnUrl_IsAddedToEndpointUsingQueryContract()
    {
        var cut = RenderLoginForm(
            endpoint: "/custom-login",
            returnUrl: "/home");

        var action = cut
            .Find("form")
            .GetAttribute("action");

        action.Should().Contain(
            $"{UAuthConstants.Query.ReturnUrl}=");

        action.Should().Contain(
            Uri.EscapeDataString("/home"));
    }

    [Fact]
    public void ReturnUrl_IsNotAddedUsingFormContractNameAsQueryParameter()
    {
        UAuthConstants.Query.ReturnUrl
            .Should()
            .NotBe(UAuthConstants.Form.ReturnUrl);

        var cut = RenderLoginForm(
            endpoint: "/custom-login",
            returnUrl: "/home");

        var action = cut
            .Find("form")
            .GetAttribute("action");

        action.Should().Contain(
            $"{UAuthConstants.Query.ReturnUrl}=");
    }

    // -----------------------------------------------------------------
    // Effective return URL
    // -----------------------------------------------------------------

    [Fact]
    public void DirectCommit_WithExplicitReturnUrl_UsesExplicitReturnUrl()
    {
        Navigate("/login");

        var cut = RenderLoginForm(
            returnUrl: "/dashboard",
            submitMode: UAuthSubmitMode.DirectCommit);

        var input = cut.Find(
            $"input[name='{UAuthConstants.Form.ReturnUrl}']");

        input.GetAttribute("value")
            .Should()
            .Be("/dashboard");
    }

    [Fact]
    public void PasswordDirectCommit_WithoutExplicitReturnUrl_UsesCurrentNavigationUri()
    {
        Navigate("/login?foo=bar");

        var cut = RenderLoginForm(
            submitMode: UAuthSubmitMode.DirectCommit,
            loginType: UAuthLoginType.Password);

        var input = cut.Find(
            $"input[name='{UAuthConstants.Form.ReturnUrl}']");

        input.GetAttribute("value")
            .Should()
            .Be(Nav.Uri);
    }

    // -----------------------------------------------------------------
    // Device initialization
    // -----------------------------------------------------------------

    [Fact]
    public void Render_RequestsDeviceIdExactlyOnce()
    {
        RenderLoginForm();

        _deviceIdProvider.Verify(
            x => x.GetOrCreateAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -----------------------------------------------------------------
    // Password must not touch Hub
    // -----------------------------------------------------------------

    [Fact]
    public void PasswordLogin_DoesNotResolveHubCredentials()
    {
        RenderLoginForm(
            loginType: UAuthLoginType.Password);

        _hubCredentialResolver.Verify(
            x => x.ResolveAsync(
                It.IsAny<HubSessionId>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void PasswordLogin_DoesNotReadHubState()
    {
        RenderLoginForm(
            loginType: UAuthLoginType.Password);

        _hubFlowReader.Verify(
            x => x.GetStateAsync(
                It.IsAny<HubSessionId>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -----------------------------------------------------------------
    // PKCE / Hub
    // -----------------------------------------------------------------

    [Fact]
    public void Pkce_WhenHubIsNotSupported_ThrowsInvalidOperationException()
    {
        _hubCapabilities
            .SetupGet(x => x.SupportsPkce)
            .Returns(false);

        var act = () => RenderLoginForm(
            loginType: UAuthLoginType.Pkce);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*PKCE login requires UAuthHub*");
    }

    [Fact]
    public void Pkce_WithExplicitHubSessionId_ResolvesCredentials()
    {
        var hubId = HubSessionId.New();

        SetupPkceHub(hubId);

        RenderPkceLoginForm(hubId);

        _hubCredentialResolver.Verify(
            x => x.ResolveAsync(
                hubId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Pkce_WithExplicitHubSessionId_ReadsHubState()
    {
        var hubId = HubSessionId.New();

        SetupPkceHub(hubId);

        RenderPkceLoginForm(hubId);

        _hubFlowReader.Verify(
            x => x.GetStateAsync(
                hubId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Pkce_WithHubInQuery_ResolvesCredentials()
    {
        var hubId = HubSessionId.New();

        SetupPkceHub(hubId);

        Navigate(
            $"/login?{UAuthConstants.Query.Hub}=" +
            Uri.EscapeDataString(hubId.Value));

        RenderLoginForm(
            loginType: UAuthLoginType.Pkce);

        _hubCredentialResolver.Verify(
            x => x.ResolveAsync(
                hubId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Pkce_WithHubInQuery_ReadsHubState()
    {
        var hubId = HubSessionId.New();

        SetupPkceHub(hubId);

        Navigate(
            $"/login?{UAuthConstants.Query.Hub}=" +
            Uri.EscapeDataString(hubId.Value));

        RenderLoginForm(
            loginType: UAuthLoginType.Pkce);

        _hubFlowReader.Verify(
            x => x.GetStateAsync(
                hubId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Pkce_WithInvalidHubInQuery_DoesNotResolveCredentials()
    {
        Navigate(
            $"/login?{UAuthConstants.Query.Hub}=invalid-hub");

        RenderLoginForm(
            loginType: UAuthLoginType.Pkce);

        _hubCredentialResolver.Verify(
            x => x.ResolveAsync(
                It.IsAny<HubSessionId>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Pkce_WithInvalidHubInQuery_DoesNotReadHubState()
    {
        Navigate(
            $"/login?{UAuthConstants.Query.Hub}=invalid-hub");

        RenderLoginForm(
            loginType: UAuthLoginType.Pkce);

        _hubFlowReader.Verify(
            x => x.GetStateAsync(
                It.IsAny<HubSessionId>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Pkce_ExplicitHubSessionId_TakesPrecedenceOverQueryHub()
    {
        var parameterHub = HubSessionId.New();
        var queryHub = HubSessionId.New();

        SetupPkceHub(parameterHub);

        Navigate(
            $"/login?{UAuthConstants.Query.Hub}=" +
            Uri.EscapeDataString(queryHub.Value));

        RenderPkceLoginForm(parameterHub);

        _hubCredentialResolver.Verify(
            x => x.ResolveAsync(
                parameterHub,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _hubCredentialResolver.Verify(
            x => x.ResolveAsync(
                queryHub,
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Pkce_WithCredentials_RendersAuthorizationCode()
    {
        var hubId = HubSessionId.New();

        SetupPkceHub(
            hubId,
            authorizationCode: "auth-code-123",
            codeVerifier: "verifier-456");

        var cut = RenderPkceLoginForm(hubId);

        var input = cut.Find(
            "input[name='authorization_code']");

        input.GetAttribute("value")
            .Should()
            .Be("auth-code-123");
    }

    [Fact]
    public void Pkce_WithCredentials_RendersCodeVerifier()
    {
        var hubId = HubSessionId.New();

        SetupPkceHub(
            hubId,
            authorizationCode: "auth-code-123",
            codeVerifier: "verifier-456");

        var cut = RenderPkceLoginForm(hubId);

        var input = cut.Find(
            "input[name='code_verifier']");

        input.GetAttribute("value")
            .Should()
            .Be("verifier-456");
    }

    [Fact]
    public void Pkce_WithoutExplicitReturnUrl_UsesHubFlowReturnUrl()
    {
        var hubId = HubSessionId.New();

        SetupPkceHub(
            hubId,
            returnUrl: "/from-hub");

        var cut = RenderPkceLoginForm(
            hubId,
            submitMode: UAuthSubmitMode.DirectCommit);

        var input = cut.Find(
            $"input[name='{UAuthConstants.Form.ReturnUrl}']");

        input.GetAttribute("value")
            .Should()
            .Be("/from-hub");
    }

    [Fact]
    public void Pkce_ExplicitReturnUrl_TakesPrecedenceOverHubFlowReturnUrl()
    {
        var hubId = HubSessionId.New();

        SetupPkceHub(
            hubId,
            returnUrl: "/from-hub");

        var cut = RenderPkceLoginForm(
            hubId,
            returnUrl: "/explicit",
            submitMode: UAuthSubmitMode.DirectCommit);

        var input = cut.Find(
            $"input[name='{UAuthConstants.Form.ReturnUrl}']");

        input.GetAttribute("value")
            .Should()
            .Be("/explicit");
    }

    [Fact]
    public void PkceEndpoint_WithReturnUrl_UsesQueryReturnUrlContract()
    {
        var hubId = HubSessionId.New();

        SetupPkceHub(
            hubId,
            returnUrl: "/dashboard");

        var cut = RenderPkceLoginForm(hubId);

        var action = cut
            .Find("form")
            .GetAttribute("action");

        action.Should()
            .Contain($"{UAuthConstants.Query.ReturnUrl}=");

        action.Should()
            .Contain(Uri.EscapeDataString("/dashboard"));
    }

    [Fact]
    public void PkceEndpoint_WithHub_UsesUAuthHubQueryContract()
    {
        var hubId = HubSessionId.New();

        SetupPkceHub(hubId);

        var cut = RenderPkceLoginForm(hubId);

        var action = cut
            .Find("form")
            .GetAttribute("action");

        action.Should()
            .Contain($"{UAuthConstants.Query.Hub}=");
    }

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    private IRenderedComponent<UAuthLoginForm> RenderLoginForm(
        string? identifier = "alice",
        string? secret = "secret",
        string? endpoint = null,
        string? returnUrl = null,
        UAuthLoginType loginType = UAuthLoginType.Password,
        UAuthSubmitMode submitMode = UAuthSubmitMode.TryAndCommit,
        bool allowEnterKeyToSubmit = true,
        RenderFragment? childContent = null)
    {
        return Render<UAuthLoginForm>(parameters =>
        {
            parameters
                .Add(x => x.Identifier, identifier)
                .Add(x => x.Secret, secret)
                .Add(x => x.LoginType, loginType)
                .Add(x => x.SubmitMode, submitMode)
                .Add(
                    x => x.AllowEnterKeyToSubmit,
                    allowEnterKeyToSubmit);

            if (endpoint is not null)
                parameters.Add(x => x.Endpoint, endpoint);

            if (returnUrl is not null)
                parameters.Add(x => x.ReturnUrl, returnUrl);

            if (childContent is not null)
                parameters.Add(
                    x => x.ChildContent,
                    childContent);
        });
    }

    private void SetupPkceHub(
    HubSessionId hubId,
    string authorizationCode = "authorization-code",
    string codeVerifier = "code-verifier",
    string? returnUrl = "/home",
    bool exists = true,
    bool active = true)
    {
        var credentials = new HubCredentials
        {
            AuthorizationCode = authorizationCode,
            CodeVerifier = codeVerifier
        };

        var state = new HubFlowState
        {
            HubSessionId = hubId,
            ReturnUrl = returnUrl,
            Exists = exists,
            IsActive = active
        };

        _hubCredentialResolver
            .Setup(x => x.ResolveAsync(
                hubId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);

        _hubFlowReader
            .Setup(x => x.GetStateAsync(
                hubId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(state);
    }

    private IRenderedComponent<UAuthLoginForm> RenderPkceLoginForm(
        HubSessionId hubId,
        string? returnUrl = null,
        UAuthSubmitMode submitMode = UAuthSubmitMode.TryAndCommit)
    {
        return Render<UAuthLoginForm>(parameters =>
        {
            parameters
                .Add(x => x.Identifier, "alice")
                .Add(x => x.Secret, "secret")
                .Add(x => x.HubSessionId, hubId)
                .Add(x => x.LoginType, UAuthLoginType.Pkce)
                .Add(x => x.SubmitMode, submitMode);

            if (returnUrl is not null)
            {
                parameters.Add(
                    x => x.ReturnUrl,
                    returnUrl);
            }
        });
    }

    private void Navigate(string relativeUri)
    {
        Nav.NavigateTo(relativeUri);
    }

    private NavigationManager Nav =>
        Services.GetRequiredService<NavigationManager>();
}
