using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;

// TODO: Add multi tab single refresh support
internal sealed class SessionCoordinator : ISessionCoordinator
{
    private readonly IUAuthClient _client;
    private readonly NavigationManager _navigation;
    private readonly UAuthClientOptions _options;
    private readonly UAuthClientDiagnostics _diagnostics;
    private readonly IClock _clock;

    private CancellationTokenSource? _cts;

    public event Action? ReauthRequired;

    public SessionCoordinator(IUAuthClient client, NavigationManager navigation, IOptions<UAuthClientOptions> options, UAuthClientDiagnostics diagnostics, IClock clock)
    {
        _client = client;
        _navigation = navigation;
        _options = options.Value;
        _diagnostics = diagnostics;
        _clock = clock;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.AutoRefresh.Enabled)
            return;

        if (_cts is not null)
            return;

        _diagnostics.MarkStarted();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _ = RunAsync(_cts.Token);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var interval = _options.AutoRefresh.Interval ?? TimeSpan.FromMinutes(5);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(interval, ct);
                await TickAsync();

                if (_diagnostics.IsTerminated)
                    return;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public Task StopAsync()
    {
        _diagnostics.MarkStopped();
        _cts?.Cancel();
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    internal async Task TickAsync()
    {
        _diagnostics.MarkAutomaticRefresh();

        var result = await _client.Flows.RefreshAsync(true);

        if (result.Outcome != RefreshOutcome.ReauthRequired)
            return;

        if (result.Outcome == RefreshOutcome.ReauthRequired)
        {
            switch (_options.Reauth.Behavior)
            {
                case ReauthBehavior.Redirect: _navigation.NavigateTo(_options.Reauth.RedirectPath ?? _options.Endpoints.Login, forceLoad: true);
                    break;

                case ReauthBehavior.RaiseEvent:
                    ReauthRequired?.Invoke();
                    break;
            }

            _diagnostics.MarkTerminated(CoordinatorTerminationReason.ReauthRequired);
        }
    }
}
