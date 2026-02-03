using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class BlazorServerSessionCoordinatorTests
{
    //[Fact]
    //public async Task StartAsync_MarksStarted_AndAutomaticRefresh()
    //{
    //    var diagnostics = new UAuthClientDiagnostics();
    //    var client = new FakeFlowClient(RefreshOutcome.NoOp);
    //    var nav = new TestNavigationManager();

    //    var options = Options.Create(new UAuthClientOptions
    //    {
    //        Refresh = { Interval = TimeSpan.FromMilliseconds(10) }
    //    });

    //    var coordinator = new BlazorServerSessionCoordinator(new UAuthClient(client),
    //        nav,
    //        options,
    //        diagnostics);

    //    await coordinator.StartAsync();
    //    await Task.Delay(30);
    //    await coordinator.StopAsync();

    //    Assert.Equal(1, diagnostics.StartCount);
    //    Assert.True(diagnostics.AutomaticRefreshCount >= 1);
    //}

    //[Fact]
    //public async Task ReauthRequired_ShouldTerminateAndNavigate()
    //{
    //    var diagnostics = new UAuthClientDiagnostics();
    //    var client = new FakeFlowClient(RefreshOutcome.ReauthRequired);
    //    var nav = new TestNavigationManager();

    //    var options = Options.Create(new UAuthClientOptions
    //    {
    //        Refresh = { Interval = TimeSpan.FromMilliseconds(5) },
    //        Reauth =
    //        {
    //            Behavior = ReauthBehavior.RedirectToLogin,
    //            LoginPath = "/login"
    //        }
    //    });

    //    var coordinator = new BlazorServerSessionCoordinator(new UAuthClient(client),
    //        nav,
    //        options,
    //        diagnostics);

    //    await coordinator.StartAsync();
    //    await Task.Delay(20);

    //    Assert.True(diagnostics.IsTerminated);
    //    Assert.Equal(CoordinatorTerminationReason.ReauthRequired, diagnostics.TerminationReason);
    //    Assert.Equal("/login", nav.LastNavigatedTo);
    //}

    //[Fact]
    //public async Task StopAsync_ShouldMarkStopped()
    //{
    //    var diagnostics = new UAuthClientDiagnostics();
    //    var client = new FakeFlowClient();
    //    var nav = new TestNavigationManager();

    //    var options = Options.Create(new UAuthClientOptions());

    //    var coordinator = new BlazorServerSessionCoordinator(new UAuthClient(client),
    //        nav,
    //        options,
    //        diagnostics);

    //    await coordinator.StartAsync();
    //    await coordinator.StopAsync();

    //    Assert.Equal(1, diagnostics.StopCount);
    //}
}
