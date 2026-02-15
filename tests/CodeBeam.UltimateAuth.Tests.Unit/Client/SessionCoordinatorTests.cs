using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Moq;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class SessionCoordinatorTests
{
    [Fact]
    public async Task StartAsync_should_not_start_when_auto_refresh_disabled()
    {
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

        var coordinator = new SessionCoordinator(client.Object, nav.Object, options, diagnostics);
        await coordinator.StartAsync();

        Assert.False(diagnostics.IsRunning);
    }

    [Fact]
    public async Task ReauthRequired_should_raise_event()
    {
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
                Interval = TimeSpan.FromMilliseconds(10)
            },
            Reauth = new UAuthClientReauthOptions
            {
                Behavior = ReauthBehavior.RaiseEvent
            }
        });

        var coordinator = new SessionCoordinator(client.Object, nav.Object, options, diagnostics);
        var triggered = false;
        coordinator.ReauthRequired += () => triggered = true;

        await coordinator.StartAsync();
        await Task.Delay(50);

        Assert.True(triggered);
        Assert.True(diagnostics.IsTerminated);
    }
}
