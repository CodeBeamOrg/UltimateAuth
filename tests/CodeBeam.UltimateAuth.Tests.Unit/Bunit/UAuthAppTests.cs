using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthAppTests
{

    private (BunitContext ctx, Mock<IUAuthStateManager> stateManager, Mock<IUAuthClientBootstrapper> bootstrapper, Mock<ISessionCoordinator> coordinator)
    
        CreateUAuthAppTestContext(UAuthState state, bool authenticated = true)
    {
        var ctx = new BunitContext();

        var stateManager = new Mock<IUAuthStateManager>();
        stateManager.Setup(x => x.State).Returns(state);
        stateManager.Setup(x => x.EnsureAsync(It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var bootstrapper = new Mock<IUAuthClientBootstrapper>();
        bootstrapper.Setup(x => x.EnsureStartedAsync())
            .Returns(Task.CompletedTask);

        var coordinator = new Mock<ISessionCoordinator>();
        coordinator.Setup(x => x.StartAsync()).Returns(Task.CompletedTask);
        coordinator.Setup(x => x.StopAsync()).Returns(Task.CompletedTask);

        ctx.Services.AddSingleton(stateManager.Object);
        ctx.Services.AddSingleton(bootstrapper.Object);
        ctx.Services.AddSingleton(coordinator.Object);

        var auth = ctx.AddAuthorization();
        if (authenticated)
            auth.SetAuthorized("test-user");
        else
            auth.SetNotAuthorized();

        return (ctx, stateManager, bootstrapper, coordinator);
    }

    [Fact]
    public async Task Should_Initialize_And_Bootstrap_On_First_Render()
    {
        var state = TestAuthState.Anonymous();
        var (ctx, stateManager, bootstrapper, _) = CreateUAuthAppTestContext(state);
        var cut = ctx.Render<UAuthApp>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        bootstrapper.Verify(x => x.EnsureStartedAsync(), Times.Once);
        stateManager.Verify(x => x.EnsureAsync(It.IsAny<bool>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Should_Start_Coordinator_When_Authenticated()
    {
        var state = TestAuthState.Authenticated();
        var (ctx, _, _, coordinator) = CreateUAuthAppTestContext(state);
        var cut = ctx.Render<UAuthApp>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        coordinator.Verify(x => x.StartAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Stop_Coordinator_When_State_Cleared()
    {
        var state = TestAuthState.Authenticated();
        var (ctx, _, _, coordinator) = CreateUAuthAppTestContext(state);
        var cut = ctx.Render<UAuthApp>();
        state.Clear();
        await cut.InvokeAsync(() => Task.CompletedTask);

        coordinator.Verify(x => x.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Stop_Coordinator_On_Dispose()
    {
        var state = TestAuthState.Authenticated();
        var (ctx, _, _, coordinator) = CreateUAuthAppTestContext(state);
        var cut = ctx.Render<UAuthApp>();
        await cut.Instance.DisposeAsync();

        coordinator.Verify(x => x.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Call_Ensure_When_State_Is_Stale()
    {
        var state = TestAuthState.Authenticated();
        state.MarkStale();
        var (ctx, stateManager, _, _) = CreateUAuthAppTestContext(state);
        var cut = ctx.Render<UAuthApp>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        stateManager.Verify(x => x.EnsureAsync(true), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Should_Invoke_Callback_On_Reauth()
    {
        var state = TestAuthState.Authenticated();
        var (ctx, _, _, coordinator) = CreateUAuthAppTestContext(state);
        var called = false;
        var cut = ctx.Render<UAuthApp>(p => p.Add(x => x.OnReauthRequired, EventCallback.Factory.Create(this, () => called = true)));

        await cut.InvokeAsync(() => Task.CompletedTask);
        coordinator.Raise(x => x.ReauthRequired += null);
        await cut.InvokeAsync(() => Task.CompletedTask);

        called.Should().BeTrue();
    }
}
