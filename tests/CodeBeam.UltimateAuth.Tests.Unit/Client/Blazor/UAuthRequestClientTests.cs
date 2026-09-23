using Bunit;
using CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Options;
using FluentAssertions;
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

    private UAuthRequestClient CreateSut(UAuthClientProfile profile = UAuthClientProfile.BlazorWasm)
    {
        var options = Options.Create(new UAuthClientOptions
        {
            ClientProfile = profile
        });

        return new UAuthRequestClient(JSInterop.JSRuntime, _bootstrapper.Object, options);
    }

    [Fact]
    public async Task NavigateAsync_InvokesUAuthPost()
    {
        var sut = CreateSut();
        await sut.NavigateAsync("/login");

        var invocation = JSInterop.Invocations.Single(x => x.Identifier == "uauth.post");
        invocation.Arguments.Should().ContainSingle();

        _bootstrapper.Verify(x => x.EnsureStartedAsync(), Times.Once);
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
        var result = await sut.SendFormAsync("/login");
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
