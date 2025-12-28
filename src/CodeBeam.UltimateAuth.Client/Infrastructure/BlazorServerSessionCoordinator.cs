using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Infrastructure
{
    internal sealed class BlazorServerSessionCoordinator : ISessionCoordinator
    {
        private readonly IUAuthClient _client;
        private readonly NavigationManager _navigation;
        private readonly UAuthClientOptions _options;

        private PeriodicTimer? _timer;
        private CancellationTokenSource? _cts;

        public BlazorServerSessionCoordinator(IUAuthClient client, NavigationManager navigation, IOptions<UAuthClientOptions> options)
        {
            _client = client;
            _navigation = navigation;
            _options = options.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_timer is not null)
                return;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var interval = _options.Refresh.Interval
                ?? TimeSpan.FromMinutes(5);

            _timer = new PeriodicTimer(interval);

            _ = RunAsync(_cts.Token);
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                while (await _timer!.WaitForNextTickAsync(ct))
                {
                    var result = await _client.RefreshAsync();

                    if (result.Outcome == RefreshOutcome.ReauthRequired)
                    {
                        _navigation.NavigateTo(
                            _options.Endpoints.Login,
                            forceLoad: true);

                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        }

        public Task StopAsync()
        {
            _cts?.Cancel();
            _timer?.Dispose();
            _timer = null;
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }
    }
}
