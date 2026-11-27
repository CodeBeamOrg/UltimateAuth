using AwesomeAssertions;
using Bunit;
using Bunit.TestDoubles;
using CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthRequestClientTests : BunitContext
{
    private readonly Mock<IUAuthClientBootstrapper> _bootstrapper = new();

    public UAuthRequestClientTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        _bootstrapper
            .Setup(x => x.EnsureStartedAsync())
            .Returns(Task.CompletedTask);
    }

    private UAuthRequestClient CreateSut(UAuthApplicationProfile profile = UAuthApplicationProfile.BlazorWasm, string? basePath = null)
    {
        var options = new UAuthClientOptions
        {
            ApplicationProfile = profile
        };

        if (basePath is not null)
            options.Endpoints.BasePath = basePath;

        var navigation = Services.GetRequiredService<NavigationManager>();

        return new UAuthRequestClient(JSInterop.JSRuntime, _bootstrapper.Object, Options.Create(options), new InMemoryUAuthTokenStore(), navigation);
    }

    [Fact]
    public async Task NavigateAsync_InvokesUAuthPost()
    {
        var sut = CreateSut();
        var ct = Xunit.TestContext.Current.CancellationToken;

        await sut.NavigateAsync("/login", ct: ct);

        var invocation = JSInterop.Invocations.Single(x => x.Identifier == "uauth.post");

        invocation.Arguments.Should().ContainSingle();

        _bootstrapper.Verify(x => x.EnsureStartedAsync(ct), Times.Once);
    }

    [Fact]
    public async Task SendFormAsync_ReturnsTransportResult()
    {
        var expected = CreateTransportResult(200);

        JSInterop
            .Setup<UAuthTransportResult>(
                "uauth.post",
                invocation => true)
            .SetResult(expected);

        var sut = CreateSut();
        var result = await sut.SendFormAsync("/login", ct: Xunit.TestContext.Current.CancellationToken);
        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task SendFormAsync_WhenJavascriptReturnsNull_ThrowsProtocolException()
    {
        JSInterop
            .Setup<UAuthTransportResult>("uauth.post")
            .SetResult(null!);

        var sut = CreateSut();

        var act = () => sut.SendFormAsync("/login");

        await act.Should()
            .ThrowAsync<UAuthProtocolException>()
            .WithMessage("Invalid error response format.");
    }

    [Fact]
    public async Task SendJsonAsync_WhenStatusIsZero_ThrowsTransportException()
    {
        JSInterop
            .Setup<UAuthTransportResult>(
                "uauth.postJson",
                _ => true)
            .SetResult(CreateTransportResult(0));

        var sut = CreateSut();

        var act = () => sut.SendJsonAsync("/api/login", new { Identifier = "alice" });

        await act.Should().ThrowAsync<UAuthTransportException>().WithMessage("Network error.");
    }

    [Fact]
    public async Task SendFormAsync_WhenCancellationAlreadyRequested_DoesNotBootstrapOrInvokeJavascript()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = CreateSut();
        var act = () => sut.SendFormAsync("/login", ct: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        _bootstrapper.Verify(x => x.EnsureStartedAsync(), Times.Never);

        JSInterop.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task TryAndCommitAsync_WhenCancellationAlreadyRequested_DoesNotBootstrapOrInvokeJavascript()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = CreateSut();

        var act = () => sut.TryAndCommitAsync<TestTryResult>(
            "/login/try",
            "/login/commit",
            new { Identifier = "alice" },
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();

        _bootstrapper.Verify(x => x.EnsureStartedAsync(), Times.Never);

        JSInterop.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task SendFormAsync_WhenBootstrapperFails_DoesNotInvokeJavascript()
    {
        _bootstrapper
            .Setup(x => x.EnsureStartedAsync())
            .ThrowsAsync(new InvalidOperationException("bootstrap failed"));

        var sut = CreateSut();
        var act = () => sut.SendFormAsync("/login");
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bootstrap failed");

        JSInterop.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task TryAndCommitThrottleKeepsStatusAndRetryGuidance()
    {
        JSInterop.Setup<System.Text.Json.JsonElement>("uauth.tryAndCommit", _ => true)
            .SetResult(System.Text.Json.JsonSerializer.SerializeToElement(new { status = 429, retryAfter = "45", isSuccess = false }));
        var error = await Assert.ThrowsAsync<UAuthTransportException>(() =>
            CreateSut().TryAndCommitAsync<CodeBeam.UltimateAuth.Core.Contracts.TryLoginResult>("/try", "/commit", new { }, ct: Xunit.TestContext.Current.CancellationToken));
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, error.StatusCode);
        Assert.Equal("rate_limited", error.Code);
        Assert.Equal(TimeSpan.FromSeconds(45), error.RetryAfter);
    }

    [Fact]
    public async Task TryAndCommitStillDeserializesSuccessfulPreview()
    {
        JSInterop.Setup<System.Text.Json.JsonElement>("uauth.tryAndCommit", _ => true)
            .SetResult(System.Text.Json.JsonSerializer.SerializeToElement(new { isSuccess = true, previewReceipt = "receipt" }));
        var result = await CreateSut().TryAndCommitAsync<CodeBeam.UltimateAuth.Core.Contracts.TryLoginResult>("/try", "/commit", new { }, ct: Xunit.TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Equal("receipt", result.PreviewReceipt);
    }

    [Fact]
    public async Task ObservedLogoutReturnsHttpRejectionRatherThanSubmittingAnUnobservedForm()
    {
        JSInterop.Setup<UAuthTransportResult>("uauth.post", _ => true)
            .SetResult(new UAuthTransportResult { Status = 429, RetryAfter = "15" });
        var result = await CreateSut().SendFormAsync("/auth/logout", null, navigateOnSuccess: true, ct: Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(429, result.Status);
        Assert.Equal("15", result.RetryAfter);
        var invocation = JSInterop.Invocations.Single(x => x.Identifier == "uauth.post");
        Assert.Equal("logout", GetProperty<string>(invocation.Arguments.Single()!, "mode"));
    }

    [Theory]
    [InlineData("/auth", null)]
    [InlineData("auth", null)]
    [InlineData("https://example.com/auth", "https://example.com")]
    [InlineData("http://localhost:5000/auth", "http://localhost:5000")]
    public async Task SendFormAsync_ResolvesTrustedOriginCorrectly(string basePath, string? expectedOrigin)
    {
        // Arrange
        JSInterop
            .Setup<UAuthTransportResult>("uauth.post", _ => true)
            .SetResult(CreateTransportResult(200));

        var sut = CreateSut(basePath: basePath);

        // Act
        await sut.SendFormAsync("/auth/validate", ct: Xunit.TestContext.Current.CancellationToken);

        // Assert
        var invocation = JSInterop.Invocations
            .Single(x => x.Identifier == "uauth.post");

        var options = invocation.Arguments.Single()!;

        GetProperty<string>(options, "url")
            .Should().Be("/auth/validate");

        GetProperty<string?>(options, "trustedOrigin")
            .Should().Be(expectedOrigin);
    }


    [Theory]
    [InlineData(true, "logout")]
    [InlineData(false, "logout-observe")]
    public async Task LogoutTransport_UsesExpectedJavascriptMode(bool navigate, string expectedMode)
    {
        var expected = new UAuthTransportResult
        {
            Ok = true,
            Status = 200
        };

        JSInterop
            .Setup<UAuthTransportResult>("uauth.post", _ => true)
            .SetResult(expected);

        var result = await CreateSut().SendFormAsync(
            "/auth/logout",
            null,
            navigateOnSuccess: navigate, Xunit.TestContext.Current.CancellationToken);

        result.Should().BeSameAs(expected);

        var invocation = JSInterop.Invocations
            .Single(x => x.Identifier == "uauth.post");

        GetProperty<string>(
            invocation.Arguments.Single()!, "mode")
            .Should().Be(expectedMode);
    }

    [Fact]
    public async Task LogoutTransport_RejectsNullJavascriptResponse()
    {
        JSInterop
            .Setup<UAuthTransportResult>("uauth.post", _ => true)
            .SetResult(null!);

        var act = () => CreateSut().SendFormAsync(
            "/auth/logout",
            null,
            navigateOnSuccess: false);

        await act.Should().ThrowAsync<UAuthProtocolException>()
            .WithMessage("*logout transport response*");
    }

    [Fact]
    public async Task LogoutTransport_RejectsUnconfirmedNetworkResult()
    {
        JSInterop
            .Setup<UAuthTransportResult>("uauth.post", _ => true)
            .SetResult(new UAuthTransportResult
            {
                Status = 0
            });

        var act = () => CreateSut().SendFormAsync(
            "/auth/logout",
            null,
            navigateOnSuccess: true);

        await act.Should()
            .ThrowAsync<UAuthTransportException>()
            .WithMessage("Could not confirm server logout.");
    }

    [Fact]
    public async Task GetLoginTransportAsync_DeserializesCapabilities()
    {
        JSInterop
            .Setup<UAuthTransportResult>("uauth.getLoginTransport", _ => true)
            .SetResult(new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                Body = System.Text.Json.JsonSerializer.SerializeToElement(
                    new
                    {
                        supported = true,
                        navigationSupported = false,
                        accessTokenInHeader = true
                    })
            });

        var result = await CreateSut().GetLoginTransportAsync("/auth/login/transport", Xunit.TestContext.Current.CancellationToken);

        result.Supported.Should().BeTrue();
        result.NavigationSupported.Should().BeFalse();
        result.AccessTokenInHeader.Should().BeTrue();

        JSInterop.VerifyInvoke("uauth.getLoginTransport", 1);
    }

    [Theory]
    [InlineData(false, 200)]
    [InlineData(true, 401)]
    public async Task GetLoginTransportAsync_RejectsFailedResponse(bool ok, int status)
    {
        JSInterop
            .Setup<UAuthTransportResult>("uauth.getLoginTransport", _ => true)
            .SetResult(new UAuthTransportResult
            {
                Ok = ok,
                Status = status
            });

        var act = () => CreateSut().GetLoginTransportAsync(
            "/auth/login/transport");

        await act.Should().ThrowAsync<UAuthProtocolException>();
    }

    [Theory]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example/")]
    [InlineData(@"\\evil.example\")]
    public async Task NavigateToUrlAsync_RejectsUnsafeDestination(
        string destination)
    {
        var act = () => CreateSut()
            .NavigateToUrlAsync(destination);

        await act.Should().ThrowAsync<UAuthProtocolException>();
    }

    [Fact]
    public async Task SendFormAsync_WithExplicitToken_ForwardsTokenToJavascript()
    {
        JSInterop
            .Setup<UAuthTransportResult>("uauth.post", _ => true)
            .SetResult(new UAuthTransportResult
            {
                Ok = true,
                Status = 200
            });

        await CreateSut().SendFormAsync(
            "/auth/validate",
            form: null,
            accessToken: "jwt-value", Xunit.TestContext.Current.CancellationToken);

        var invocation = JSInterop.Invocations
            .Single(x => x.Identifier == "uauth.post");

        var argument = invocation.Arguments.Single()!;

        GetProperty<string>(argument, "mode")
            .Should().Be("fetch");

        GetProperty<string?>(argument, "accessToken")
            .Should().Be("jwt-value");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NavigateToPkceReturnUrlAsync_UsesExpectedNavigationMode(
    bool crossOrigin)
    {
        var navigation = Services.GetRequiredService<NavigationManager>();

        var target = crossOrigin
            ? "https://client.example.com/home"
            : new Uri(new Uri(navigation.BaseUri), "home").AbsoluteUri;

        var sut = CreateSut();

        await sut.NavigateToPkceReturnUrlAsync(
            target,
            target,
            Xunit.TestContext.Current.CancellationToken);

        navigation.Uri.Should().Be(target);

        var fakeNavigation = (BunitNavigationManager)navigation;

        var entry = Assert.Single(fakeNavigation.History);

        entry.Options.ForceLoad.Should().Be(crossOrigin);
    }

    [Theory]
    [InlineData("/home", "/home")]
    [InlineData("https://client.example.com/home", "/home")]
    [InlineData("/home", "https://client.example.com/home")]
    [InlineData("https://evil.example.com/home", "https://client.example.com/home")]
    [InlineData("https://client.example.com/other", "https://client.example.com/home")]
    [InlineData("https://client.example.com/home?x=1", "https://client.example.com/home")]
    [InlineData("http://client.example.com/home", "https://client.example.com/home")]
    [InlineData("https://client.example.com:8443/home", "https://client.example.com/home")]
    [InlineData("ftp://client.example.com/home", "ftp://client.example.com/home")]
    [InlineData("https://user@client.example.com/home", "https://user@client.example.com/home")]
    public async Task NavigateToPkceReturnUrlAsync_RejectsInvalidDestination(
    string redirectUrl,
    string expectedReturnUrl)
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var originalUri = navigation.Uri;

        var sut = CreateSut();

        Func<Task> act = () => sut.NavigateToPkceReturnUrlAsync(
            redirectUrl,
            expectedReturnUrl);

        await act.Should()
            .ThrowAsync<UAuthProtocolException>()
            .WithMessage(
                "PKCE return URL is invalid or does not match the expected destination.");

        navigation.Uri.Should().Be(originalUri);
    }

    [Fact]
    public async Task NavigateToPkceReturnUrlAsync_WhenCancelled_DoesNotNavigate()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var navigation = Services.GetRequiredService<NavigationManager>();
        var originalUri = navigation.Uri;

        const string target = "https://client.example.com/home";

        var sut = CreateSut();

        Func<Task> act = () => sut.NavigateToPkceReturnUrlAsync(
            target,
            target,
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();

        navigation.Uri.Should().Be(originalUri);
    }


    private sealed class TestTryResult
    {
    }

    private static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance
            .GetType()
            .GetProperty(propertyName);

        property.Should().NotBeNull(
            $"JS request should contain property '{propertyName}'");

        return (T)property!.GetValue(instance)!;
    }

    private static UAuthTransportResult CreateTransportResult(int status)
    {
        return new UAuthTransportResult
        {
            Status = status
        };
    }
}
