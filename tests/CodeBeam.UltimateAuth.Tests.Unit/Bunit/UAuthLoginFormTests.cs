using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthLoginFormTests
{
    [Fact]
    public void Should_Render_Form()
    {
        using var ctx = new BunitContext();

        ctx.Services.AddSingleton(Mock.Of<IDeviceIdProvider>());
        ctx.Services.AddSingleton(Mock.Of<IUAuthClient>());
        ctx.Services.AddSingleton(Mock.Of<IHubCredentialResolver>());
        ctx.Services.AddSingleton(Mock.Of<IHubFlowReader>());
        ctx.Services.AddSingleton(Mock.Of<IHubCapabilities>());
        ctx.Services.AddSingleton<IOptions<UAuthClientOptions>>(Options.Create(new UAuthClientOptions()));

        var cut = ctx.Render<UAuthLoginForm>();

        cut.Find("form").Should().NotBeNull();
    }

    [Fact]
    public void Should_Render_Identifier_And_Secret_Inputs()
    {
        using var ctx = new BunitContext();

        ctx.Services.AddSingleton(Mock.Of<IDeviceIdProvider>());
        ctx.Services.AddSingleton(Mock.Of<IUAuthClient>());
        ctx.Services.AddSingleton(Mock.Of<IHubCredentialResolver>());
        ctx.Services.AddSingleton(Mock.Of<IHubFlowReader>());
        ctx.Services.AddSingleton(Mock.Of<IHubCapabilities>());
        ctx.Services.AddSingleton<IOptions<UAuthClientOptions>>(Options.Create(new UAuthClientOptions()));

        var cut = ctx.Render<UAuthLoginForm>(p => p
            .Add(x => x.Identifier, "user")
            .Add(x => x.Secret, "pass"));

        cut.Markup.Should().Contain("name=\"Identifier\"");
        cut.Markup.Should().Contain("name=\"Secret\"");
    }

    [Fact]
    public async Task Submit_Should_Call_TryLogin()
    {
        using var ctx = new BunitContext();

        var flowMock = new Mock<IFlowClient>();

        flowMock.Setup(x => x.TryLoginAsync(
            It.IsAny<LoginRequest>(),
            It.IsAny<UAuthSubmitMode>(),
            It.IsAny<string>()))
            .ReturnsAsync(new TryLoginResult { Success = true });

        var clientMock = new Mock<IUAuthClient>();
        clientMock.Setup(x => x.Flows).Returns(flowMock.Object);

        ctx.Services.AddSingleton(clientMock.Object);
        ctx.Services.AddSingleton(Mock.Of<IDeviceIdProvider>());
        ctx.Services.AddSingleton(Mock.Of<IHubCredentialResolver>());
        ctx.Services.AddSingleton(Mock.Of<IHubFlowReader>());
        ctx.Services.AddSingleton(Mock.Of<IHubCapabilities>());
        ctx.Services.AddSingleton<IOptions<UAuthClientOptions>>(Options.Create(new UAuthClientOptions()));

        var cut = ctx.Render<UAuthLoginForm>(p => p
            .Add(x => x.Identifier, "user")
            .Add(x => x.Secret, "pass"));

        await cut.Instance.SubmitAsync();

        flowMock.Verify(x =>
            x.TryLoginAsync(It.IsAny<LoginRequest>(), It.IsAny<UAuthSubmitMode>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Submit_Should_Throw_When_Missing_Credentials()
    {
        using var ctx = new BunitContext();

        ctx.Services.AddSingleton(Mock.Of<IUAuthClient>());
        ctx.Services.AddSingleton(Mock.Of<IDeviceIdProvider>());
        ctx.Services.AddSingleton(Mock.Of<IHubCredentialResolver>());
        ctx.Services.AddSingleton(Mock.Of<IHubFlowReader>());
        ctx.Services.AddSingleton(Mock.Of<IHubCapabilities>());
        ctx.Services.AddSingleton<IOptions<UAuthClientOptions>>(Options.Create(new UAuthClientOptions()));

        var cut = ctx.Render<UAuthLoginForm>();

        Func<Task> act = async () => await cut.Instance.SubmitAsync();

        await act.Should().ThrowAsync<UAuthValidationException>();
    }

    [Fact]
    public async Task SubmitPkce_Should_Call_TryCompletePkce()
    {
        using var ctx = new BunitContext();

        var flowMock = new Mock<IFlowClient>();

        flowMock.Setup(x => x.TryCompletePkceLoginAsync(
            It.IsAny<PkceCompleteRequest>(),
            It.IsAny<UAuthSubmitMode>()))
            .ReturnsAsync(new TryPkceLoginResult { Success = true });

        var clientMock = new Mock<IUAuthClient>();
        clientMock.Setup(x => x.Flows).Returns(flowMock.Object);

        var credResolver = new Mock<IHubCredentialResolver>();
        credResolver.Setup(x => x.ResolveAsync(It.IsAny<HubSessionId>()))
            .ReturnsAsync(new HubCredentials
            {
                AuthorizationCode = "code",
                CodeVerifier = "verifier"
            });

        var capabilities = new Mock<IHubCapabilities>();
        capabilities.Setup(x => x.SupportsPkce).Returns(true);

        ctx.Services.AddSingleton(clientMock.Object);
        ctx.Services.AddSingleton(Mock.Of<IDeviceIdProvider>());
        ctx.Services.AddSingleton(credResolver.Object);
        ctx.Services.AddSingleton(Mock.Of<IHubFlowReader>());
        ctx.Services.AddSingleton(capabilities.Object);
        ctx.Services.AddSingleton<IOptions<UAuthClientOptions>>(Options.Create(new UAuthClientOptions()));

        var hubSessionId = HubSessionId.New();

        var cut = ctx.Render<UAuthLoginForm>(p => p
            .Add(x => x.Identifier, "user")
            .Add(x => x.Secret, "pass")
            .Add(x => x.LoginType, UAuthLoginType.Pkce)
            .Add(x => x.HubSessionId, hubSessionId));

        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.Instance.SubmitAsync();

        flowMock.Verify(x =>
            x.TryCompletePkceLoginAsync(It.IsAny<PkceCompleteRequest>(), It.IsAny<UAuthSubmitMode>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Invoke_OnTryResult()
    {
        using var ctx = new BunitContext();

        var flowMock = new Mock<IFlowClient>();

        flowMock.Setup(x => x.TryLoginAsync(
            It.IsAny<LoginRequest>(),
            It.IsAny<UAuthSubmitMode>(),
            It.IsAny<string>()))
            .ReturnsAsync(new TryLoginResult { Success = true });

        var clientMock = new Mock<IUAuthClient>();
        clientMock.Setup(x => x.Flows).Returns(flowMock.Object);

        var invoked = false;

        ctx.Services.AddSingleton(clientMock.Object);
        ctx.Services.AddSingleton(Mock.Of<IDeviceIdProvider>());
        ctx.Services.AddSingleton(Mock.Of<IHubCredentialResolver>());
        ctx.Services.AddSingleton(Mock.Of<IHubFlowReader>());
        ctx.Services.AddSingleton(Mock.Of<IHubCapabilities>());
        ctx.Services.AddSingleton<IOptions<UAuthClientOptions>>(Options.Create(new UAuthClientOptions()));

        var cut = ctx.Render<UAuthLoginForm>(p => p
            .Add(x => x.Identifier, "user")
            .Add(x => x.Secret, "pass")
            .Add(x => x.OnTryResult, EventCallback.Factory.Create<IUAuthTryResult>(this, _ => invoked = true)));

        await cut.Instance.SubmitAsync();

        invoked.Should().BeTrue();
    }

    [Fact]
    public void Should_Throw_When_Pkce_Not_Supported()
    {
        using var ctx = new BunitContext();

        var capabilities = new Mock<IHubCapabilities>();
        capabilities.Setup(x => x.SupportsPkce).Returns(false);

        ctx.Services.AddSingleton(capabilities.Object);
        ctx.Services.AddSingleton(Mock.Of<IUAuthClient>());
        ctx.Services.AddSingleton(Mock.Of<IDeviceIdProvider>());
        ctx.Services.AddSingleton(Mock.Of<IHubCredentialResolver>());
        ctx.Services.AddSingleton(Mock.Of<IHubFlowReader>());
        ctx.Services.AddSingleton<IOptions<UAuthClientOptions>>(Options.Create(new UAuthClientOptions()));

        Action act = () => ctx.Render<UAuthLoginForm>(p => p
            .Add(x => x.LoginType, UAuthLoginType.Pkce));

        act.Should().Throw<InvalidOperationException>();
    }
}
