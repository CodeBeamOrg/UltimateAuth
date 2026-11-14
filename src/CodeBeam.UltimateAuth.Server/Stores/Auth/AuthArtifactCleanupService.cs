using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Stores;

internal sealed class AuthArtifactCleanupService : BackgroundService
{
    private readonly IAuthStore _store;
    private readonly TimeSpan _interval;

    public AuthArtifactCleanupService(IAuthStore store, IOptions<UAuthServerOptions> options)
    {
        _store = store;
        _interval = options.Value.AuthArtifacts.CleanupInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // A custom IAuthStore is responsible for its own expiration.
        if (_store is not InMemoryAuthStore memoryStore)
            return;

        using var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                memoryStore.CleanupExpired();
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
    }
}
