using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class SessionCoordinatorTests
{
    [Fact]
    public async Task StartAsync_should_not_start_when_auto_refresh_disabled()
    {
        var clock = new TestClock();
        var client = new Mock<IUAuthClient>();
        var nav = new Mock<NavigationManager>();
        var diagnostics = new UAuthClientDiagnostics();

        var options = Options.Create(new UAuthClientOptions
        {
            AutoRefresh = new UAuthClientAutoRefreshOptions
            {
                Enabled = false
            }
        });

        var coordinator = new SessionCoordinator(client.Object, nav.Object, options, diagnostics, clock);
        await coordinator.StartAsync();

        Assert.False(diagnostics.IsRunning);
    }

    [Fact]
    public async Task ReauthRequired_should_raise_event()
    {
        var clock = new TestClock();

        var client = new Mock<IUAuthClient>();
        var nav = new Mock<NavigationManager>();
        var diagnostics = new UAuthClientDiagnostics();

        client.Setup(x => x.Flows.RefreshAsync(true))
              .ReturnsAsync(new RefreshResult
              {
                  Outcome = RefreshOutcome.ReauthRequired
              });

        var options = Options.Create(new UAuthClientOptions
        {
            AutoRefresh = new UAuthClientAutoRefreshOptions
            {
                Enabled = true,
                Interval = TimeSpan.FromSeconds(5)
            },
            Reauth = new UAuthClientReauthOptions
            {
                Behavior = ReauthBehavior.RaiseEvent
            }
        });

        var coordinator = new SessionCoordinator(client.Object, nav.Object, options, diagnostics, clock);
        var triggered = false;
        coordinator.ReauthRequired += () => triggered = true;

        await coordinator.TickAsync();

        Assert.True(triggered);
        Assert.True(diagnostics.IsTerminated);
    }
}
