using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthAppLifecycleTests : BunitContext
{
    private readonly Mock<IUAuthStateManager> _stateManager = new();
    private readonly Mock<IUAuthClientBootstrapper> _bootstrapper = new();
    private readonly Mock<ISessionCoordinator> _coordinator = new();

    private readonly UAuthState _state = UAuthState.Anonymous();

    public UAuthAppLifecycleTests()
    {
        this.AddAuthorization();

        _stateManager
            .SetupGet(x => x.State)
            .Returns(_state);

        _bootstrapper
            .Setup(x => x.EnsureStartedAsync())
            .Returns(Task.CompletedTask);

        _stateManager
            .Setup(x => x.EnsureAsync(It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        _coordinator
            .Setup(x => x.StartAsync())
            .Returns(Task.CompletedTask);

        _coordinator
            .Setup(x => x.StopAsync())
            .Returns(Task.CompletedTask);

        Services.AddSingleton(_stateManager.Object);
        Services.AddSingleton(_bootstrapper.Object);
        Services.AddSingleton(_coordinator.Object);
    }

    [Fact]
    public void FirstRender_StartsBootstrapperAndEnsuresStateExactlyOnce()
    {
        var cut = Render<UAuthApp>(p => p
            .AddChildContent("<div>content</div>"));

        cut.WaitForAssertion(() =>
        {
            _bootstrapper.Verify(
                x => x.EnsureStartedAsync(),
                Times.Once);

            _stateManager.Verify(
                x => x.EnsureAsync(false),
                Times.Once);
        });
    }

    [Fact]
    public void FirstRender_BootstrapsBeforeEnsuringState()
    {
        var sequence = new MockSequence();

        _bootstrapper
            .InSequence(sequence)
            .Setup(x => x.EnsureStartedAsync())
            .Returns(Task.CompletedTask);

        _stateManager
            .InSequence(sequence)
            .Setup(x => x.EnsureAsync(false))
            .Returns(Task.CompletedTask);

        Render<UAuthApp>(p => p
            .AddChildContent("content"));
    }

    [Fact]
    public void FirstRender_WhenAnonymous_DoesNotStartCoordinator()
    {
        Render<UAuthApp>(p => p
            .AddChildContent("content"));

        _coordinator.Verify(
            x => x.StartAsync(),
            Times.Never);
    }

    [Fact]
    public void FirstRender_WhenAuthenticated_StartsCoordinator()
    {
        var state = AuthenticatedState();

        _stateManager
            .SetupGet(x => x.State)
            .Returns(state);

        Render<UAuthApp>(p => p
            .AddChildContent("content"));

        _coordinator.Verify(
            x => x.StartAsync(),
            Times.Once);
    }

    [Fact]
    public void AuthenticatedStateChange_StartsCoordinator()
    {
        var cut = Render<UAuthApp>(p => p
            .AddChildContent("content"));

        _coordinator.Invocations.Clear();

        ApplyAuthenticatedSnapshot(_state);

        cut.WaitForAssertion(() =>
        {
            _coordinator.Verify(
                x => x.StartAsync(),
                Times.Once);
        });
    }

    [Fact]
    public void WhenStateNeedsValidation_ForcesEnsure()
    {
        var state = AuthenticatedState();
        state.MarkStale();

        _stateManager
            .SetupGet(x => x.State)
            .Returns(state);

        Render<UAuthApp>(p => p
            .AddChildContent("content"));

        _stateManager.Verify(
            x => x.EnsureAsync(true),
            Times.AtLeastOnce);
    }

    [Fact]
    public void ReauthRequired_MarksStateStale()
    {
        var state = AuthenticatedState();

        _stateManager
            .SetupGet(x => x.State)
            .Returns(state);

        Render<UAuthApp>(p => p
            .AddChildContent("content"));

        _coordinator.Raise(x => x.ReauthRequired += null);

        _stateManager.Verify(
            x => x.MarkStale(),
            Times.Once);
    }

    [Fact]
    public void ReauthRequired_InvokesConsumerCallback()
    {
        var callbackCount = 0;

        var cut = Render<UAuthApp>(p => p
            .Add(x => x.OnReauthRequired,
                EventCallback.Factory.Create(
                    this,
                    () => callbackCount++))
            .AddChildContent("content"));

        _coordinator.Raise(x => x.ReauthRequired += null);

        cut.WaitForAssertion(() => callbackCount.Should().Be(1));
    }

    [Fact]
    public async Task Dispose_StopsStartedCoordinator()
    {
        var state = AuthenticatedState();

        _stateManager
            .SetupGet(x => x.State)
            .Returns(state);

        var cut = Render<UAuthApp>(p => p
            .AddChildContent("content"));

        _coordinator.Invocations.Clear();

        await cut.Instance.DisposeAsync();

        _coordinator.Verify(
            x => x.StopAsync(),
            Times.Once);
    }

    [Fact]
    public async Task Dispose_WhenCoordinatorStartedAfterAuthentication_StopsCoordinator()
    {
        var cut = Render<UAuthApp>(p => p
            .AddChildContent("content"));

        ApplyAuthenticatedSnapshot(_state);

        cut.WaitForAssertion(() =>
        {
            _coordinator.Verify(
                x => x.StartAsync(),
                Times.Once);
        });

        _coordinator.Invocations.Clear();

        await cut.Instance.DisposeAsync();

        _coordinator.Verify(
            x => x.StopAsync(),
            Times.Once);
    }

    private static UAuthState AuthenticatedState(
        string userKey = "user-1",
        string? userName = "alice",
        SessionState sessionState = SessionState.Active)
    {
        var state = UAuthState.Anonymous();

        state.ApplySnapshot(
            CreateAuthSnapshot(
                userKey,
                userName,
                sessionState),
            DateTimeOffset.UtcNow);

        return state;
    }

    private static AuthStateSnapshot CreateAuthSnapshot(
        string userKey = "user-1",
        string? userName = "alice",
        SessionState sessionState = SessionState.Active)
    {
        return new AuthStateSnapshot
        {
            Identity = new AuthIdentitySnapshot
            {
                UserKey = UserKey.FromString(userKey),
                Tenant = TenantKeys.Single,
                PrimaryUserName = userName,
                DisplayName = userName,
                SessionState = sessionState,
                UserStatus = UserStatus.Active
            },
            Claims = ClaimsSnapshot.From(
                (ClaimTypes.Role, "User"))
        };
    }

    private static void ApplyAuthenticatedSnapshot(UAuthState state)
    {
        state.ApplySnapshot(
            CreateAuthSnapshot(),
            DateTimeOffset.UtcNow);
    }
}
