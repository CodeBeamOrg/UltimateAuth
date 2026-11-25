using AwesomeAssertions;
using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthLoginFormSubmissionTests : BunitContext
{
    private readonly Mock<IHubCredentialResolver> _credentials = new();
    private readonly Mock<IHubFlowReader> _flows = new();
    private readonly Mock<IHubCapabilities> _capabilities = new();

    public UAuthLoginFormSubmissionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        Services.AddSingleton(Mock.Of<IDeviceIdProvider>());
        Services.AddSingleton(_credentials.Object);
        Services.AddSingleton(_flows.Object);
        Services.AddSingleton(_capabilities.Object);
        Services.AddSingleton<IOptions<UAuthClientOptions>>(Options.Create(new UAuthClientOptions()));
        Services.AddSingleton<IUAuthTokenStore, InMemoryUAuthTokenStore>();
        Services.AddSingleton<IUAuthClientEvents, UAuthClientEvents>();
    }

    private static JsonElement Json(string value) => JsonDocument.Parse(value).RootElement.Clone();

    private IRenderedComponent<UAuthLoginForm> RenderForm(UAuthSubmitMode mode = UAuthSubmitMode.TryAndCommit)
        => Render<UAuthLoginForm>(p => p.Add(x => x.SubmitMode, mode));

    private void ConfigureResult(string json)
    {
        using var document = JsonDocument.Parse(json);

        JSInterop
            .Setup<JsonElement>(
                "uauth.submitLoginForm",
                _ => true)
            .SetResult(document.RootElement.Clone());
    }

    [Theory]
    [InlineData(UAuthSubmitMode.TryOnly)]
    [InlineData(UAuthSubmitMode.TryAndCommit)]
    public async Task Password_Submit_Uses_One_Browser_Interop_Call_And_Emits_Try_Result(UAuthSubmitMode mode)
    {
        ConfigureResult("""
        {
            "isSuccess": true,
            "previewReceipt": "receipt"
        }
        """);

        IUAuthTryResult? received = null;

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.SubmitMode, mode)
            .Add(x => x.ReturnUrl, "/dashboard")
            .Add(x => x.OnTryResult, result => received = result));

        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        received.Should().BeOfType<TryLoginResult>();
        ((TryLoginResult)received!).IsSuccess.Should().BeTrue();

        JSInterop.VerifyInvoke("uauth.submitLoginForm", 1);

        var call = JSInterop.Invocations
            .Single(x => x.Identifier == "uauth.submitLoginForm");

        call.Arguments.Should().HaveCount(2);

        var options = JsonSerializer.Serialize(call.Arguments[1]);

        using var document = JsonDocument.Parse(options);
        var root = document.RootElement;

        root.GetProperty("mode")
            .GetString()
            .Should().Be(mode.ToString());

        root.GetProperty("isPkce")
            .GetBoolean()
            .Should().BeFalse();

        root.GetProperty("tryUrl")
            .GetString()
            .Should().Be("/auth/try-login");

        root.GetProperty("commitUrl")
            .GetString()
            .Should().Contain(
                $"{UAuthConstants.Query.ReturnUrl}={Uri.EscapeDataString("/dashboard")}");

        options.Should()
            .NotContain("\"Secret\"")
            .And.NotContain("\"Identifier\"");
    }

    [Fact]
    public async Task DirectCommit_Does_Not_Emit_Try_Result()
    {
        ConfigureResult("null");

        var count = 0;

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.SubmitMode, UAuthSubmitMode.DirectCommit)
            .Add(x => x.OnTryResult, _ => count++));

        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        count.Should().Be(0);

        JSInterop.VerifyInvoke("uauth.submitLoginForm", 1);

        var call = JSInterop.Invocations
            .Single(x => x.Identifier == "uauth.submitLoginForm");

        call.Arguments.Should().HaveCount(2);

        using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(call.Arguments[1]));

        document.RootElement.GetProperty("mode")
            .GetString()
            .Should().Be(nameof(UAuthSubmitMode.DirectCommit));

        cut.Instance.IsSubmitting.Should().BeFalse();
    }

    [Fact]
    public async Task Failed_Try_Result_Is_Forwarded()
    {
        ConfigureResult("""{"isSuccess":false,"reason":0,"remainingAttempts":2}""");
        IUAuthTryResult? received = null;
        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.SubmitMode, UAuthSubmitMode.TryOnly)
            .Add(x => x.OnTryResult, result => received = result));

        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        received.Should().BeOfType<TryLoginResult>();
        ((TryLoginResult)received!).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Rate_Limit_Is_Converted_To_Try_Result()
    {
        ConfigureResult("""{"status":429,"isSuccess":false,"errorCode":"rate_limited"}""");
        IUAuthTryResult? received = null;
        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.SubmitMode, UAuthSubmitMode.TryOnly)
            .Add(x => x.OnTryResult, result => received = result));

        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        received.Should().BeOfType<TryLoginResult>();
        ((TryLoginResult)received!).Status.Should().Be(429);
    }

    [Fact]
    public async Task Submitting_Changed_Is_Raised_And_Reset_After_Success()
    {
        ConfigureResult("""{"isSuccess":true}""");
        var states = new List<bool>();
        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.OnSubmittingChanged, state => states.Add(state)));

        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        states.Should().Equal(true, false);
        cut.Instance.IsSubmitting.Should().BeFalse();
    }

    [Fact]
    public async Task Submitting_Changed_Is_Reset_After_Interop_Failure()
    {
        JSInterop
            .Setup<JsonElement>(
                "uauth.submitLoginForm",
                _ => true)
            .SetException(new JSException("submit failed"));

        var states = new List<bool>();

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.OnSubmittingChanged, state => states.Add(state)));

        var act = () => cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        await act.Should()
            .ThrowAsync<JSException>()
            .WithMessage("submit failed");

        states.Should().Equal(true, false);
        cut.Instance.IsSubmitting.Should().BeFalse();

        JSInterop.VerifyInvoke("uauth.submitLoginForm", 1);
    }

    [Fact]
    public async Task Invalid_Interop_Response_Is_Rejected()
    {
        ConfigureResult("null");
        var cut = RenderForm();
        var act = () => cut.InvokeAsync(() => cut.Instance.SubmitAsync());
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Pkce_Submit_Uses_Pkce_Interop_Options_And_Result()
    {
        var hubId = HubSessionId.New();
        _capabilities.SetupGet(x => x.SupportsPkce).Returns(true);
        _credentials.Setup(x => x.ResolveAsync(hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HubCredentials { AuthorizationCode = "auth-code", CodeVerifier = "verifier" });
        ConfigureResult("""{"isSuccess":true}""");
        IUAuthTryResult? received = null;
        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.LoginType, UAuthLoginType.Pkce)
            .Add(x => x.HubSessionId, hubId)
            .Add(x => x.SubmitMode, UAuthSubmitMode.TryOnly)
            .Add(x => x.OnTryResult, result => received = result));

        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        received.Should().BeOfType<TryPkceLoginResult>();
        var options = JsonSerializer.Serialize(JSInterop.Invocations.Single(x => x.Identifier == "uauth.submitLoginForm").Arguments[1]);
        options.Should().Contain("\"isPkce\":true");
        options.Should().Contain(Uri.EscapeDataString(hubId.Value));
        options.Should().NotContain("auth-code").And.NotContain("verifier");
    }

    [Fact]
    public async Task Pkce_Without_Resolved_Credentials_Cannot_Submit()
    {
        _capabilities.SetupGet(x => x.SupportsPkce).Returns(true);
        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.LoginType, UAuthLoginType.Pkce)
            .Add(x => x.HubSessionId, HubSessionId.New()));

        var act = () => cut.InvokeAsync(() => cut.Instance.SubmitAsync());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Missing PKCE credentials.");
        JSInterop.Invocations.Should().NotContain(x => x.Identifier == "uauth.submitLoginForm");
    }

    [Fact]
    public async Task SuccessfulFetchCommit_StoresAccessToken()
    {
        var response = new
        {
            commit = new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                AccessToken = "Bearer form-jwt",
                AccessTokenFormat = "jwt",
                AccessTokenHeaderFormat = "bearer",
                AccessTokenExpiresAt =
                    DateTimeOffset.UtcNow.AddMinutes(10).ToString("O")
            },
            accessTokenInHeader = true
        };

        ConfigureResult(JsonSerializer.Serialize(response));

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.SubmitMode, UAuthSubmitMode.DirectCommit));

        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        var store = Services.GetRequiredService<IUAuthTokenStore>();

        store.Read().Should().NotBeNull();
        store.Read()!.Token.Should().Be("form-jwt");
        store.Read()!.Format.Should().Be(TokenFormat.Jwt);

        cut.Instance.IsSubmitting.Should().BeFalse();
    }

    [Fact]
    public async Task FetchCommit_WithoutRequiredToken_RejectsCompletion()
    {
        var response = new
        {
            commit = new UAuthTransportResult
            {
                Ok = true,
                Status = 200
            },
            accessTokenInHeader = true
        };

        ConfigureResult(JsonSerializer.Serialize(response));

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.SubmitMode, UAuthSubmitMode.DirectCommit));

        var act = () => cut.InvokeAsync(
            () => cut.Instance.SubmitAsync());

        await act.Should()
            .ThrowAsync<UAuthProtocolException>()
            .WithMessage("*required access token*");

        Services.GetRequiredService<IUAuthTokenStore>()
            .Read().Should().BeNull();

        cut.Instance.IsSubmitting.Should().BeFalse();
    }

    [Fact]
    public async Task PkceHandoff_RejectsDifferentReturnUrl_WithoutStoringToken()
    {
        var hubId = HubSessionId.New();

        _capabilities.SetupGet(x => x.SupportsPkce)
            .Returns(true);

        _credentials.Setup(x => x.ResolveAsync(
                hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HubCredentials
            {
                AuthorizationCode = "authorization-code",
                CodeVerifier = "verifier"
            });

        _flows.Setup(x => x.GetStateAsync(
                hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HubFlowState
            {
                HubSessionId = hubId,
                ReturnUrl = "https://localhost:6132/home"
            });

        var response = new
        {
            commit = new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                AccessToken = "Bearer hub-jwt",
                AccessTokenFormat = "jwt",
                AccessTokenHeaderFormat = "bearer",
                AccessTokenExpiresAt =
                    DateTimeOffset.UtcNow.AddMinutes(10).ToString("O"),
                Body = JsonSerializer.SerializeToElement(new
                {
                    redirectUrl = "https://evil.example/home"
                })
            },
            accessTokenInHeader = true
        };

        ConfigureResult(JsonSerializer.Serialize(response));

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.LoginType, UAuthLoginType.Pkce)
            .Add(x => x.HubSessionId, hubId)
            .Add(x => x.SubmitMode, UAuthSubmitMode.DirectCommit));

        var act = () => cut.InvokeAsync(
            () => cut.Instance.SubmitAsync());

        await act.Should().ThrowAsync<UAuthProtocolException>()
            .WithMessage("*Invalid PKCE handoff redirect*");

        Services.GetRequiredService<IUAuthTokenStore>()
            .Read().Should().BeNull();
    }

    [Fact]
    public async Task PkceHandoff_SuccessfulRedirect_DoesNotStoreTokenOrPublishLogin()
    {
        var hubId = HubSessionId.New();
        const string returnUrl = "https://localhost:6132/home";

        _capabilities.SetupGet(x => x.SupportsPkce)
            .Returns(true);

        _credentials.Setup(x => x.ResolveAsync(
                hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HubCredentials
            {
                AuthorizationCode = "auth-code",
                CodeVerifier = "verifier"
            });

        _flows.Setup(x => x.GetStateAsync(
                hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HubFlowState
            {
                HubSessionId = hubId,
                ReturnUrl = returnUrl
            });

        var events = Services.GetRequiredService<IUAuthClientEvents>();
        var published = new List<UAuthStateEvent>();

        events.StateChanged += args =>
        {
            published.Add(args.Type);
            return Task.CompletedTask;
        };

        ConfigureResult(JsonSerializer.Serialize(new
        {
            commit = new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                AccessToken = "Bearer initiating-app-jwt",
                AccessTokenFormat = "jwt",
                AccessTokenHeaderFormat = "bearer",
                AccessTokenExpiresAt =
                    DateTimeOffset.UtcNow.AddMinutes(10).ToString("O"),
                Body = JsonSerializer.SerializeToElement(new
                {
                    redirectUrl = returnUrl
                })
            },
            accessTokenInHeader = true
        }));

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.LoginType, UAuthLoginType.Pkce)
            .Add(x => x.HubSessionId, hubId)
            .Add(x => x.SubmitMode, UAuthSubmitMode.DirectCommit));

        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        var store = Services.GetRequiredService<IUAuthTokenStore>();
        store.Read().Should().BeNull();

        published.Should().BeEmpty();

        var navigation = Services.GetRequiredService<
            Microsoft.AspNetCore.Components.NavigationManager>();

        navigation.Uri.Should().Be(returnUrl);

        cut.Instance.IsSubmitting.Should().BeFalse();
    }
}
