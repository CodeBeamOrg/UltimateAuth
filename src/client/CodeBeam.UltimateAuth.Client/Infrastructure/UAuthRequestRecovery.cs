using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal sealed class UAuthRequestRecovery : IUAuthRequestRecovery
{
    private readonly IUAuthTokenStore _tokenStore;
    private readonly IUAuthRefreshCoordinator _coordinator;
    private readonly UAuthClientAutoRefreshOptions _options;

    public UAuthRequestRecovery(
        IUAuthTokenStore tokenStore,
        IUAuthRefreshCoordinator coordinator,
        IOptions<UAuthClientOptions> options)
    {
        _tokenStore = tokenStore;
        _coordinator = coordinator;
        _options = options.Value.AutoRefresh;
    }

    public async Task<UAuthTokenSnapshot> PrepareAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var snapshot = _tokenStore.Capture();

        if (!_options.Proactive ||
            snapshot.Token is null ||
            snapshot.Token.Format != TokenFormat.Jwt)
        {
            return snapshot;
        }

        if (snapshot.Token.ExpiresAt - DateTimeOffset.UtcNow > _options.RefreshBeforeExpiration)
        {
            return snapshot;
        }

        await _coordinator.RefreshIfCurrentAsync(UAuthRefreshTrigger.Proactive, snapshot, ct);

        var current = _tokenStore.Capture();

        if (current.Epoch != snapshot.Epoch)
        {
            throw new OperationCanceledException("Authentication state changed during request preparation.");
        }

        return current;
    }

    public async Task<UAuthTokenSnapshot?> RecoverAsync(UAuthTokenSnapshot rejected, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_options.Reactive || rejected.Token is null)
            return null;

        var current = _tokenStore.Capture();

        if (current.Epoch != rejected.Epoch)
            return null;

        if (CanReplay(rejected, current))
            return current;

        await _coordinator.RefreshIfCurrentAsync(UAuthRefreshTrigger.Reactive, rejected, ct);

        current = _tokenStore.Capture();

        return CanReplay(rejected, current)
            ? current
            : null;
    }

    private static bool CanReplay(UAuthTokenSnapshot rejected, UAuthTokenSnapshot current)
    {
        return current.Epoch == rejected.Epoch &&
               current.Generation != rejected.Generation &&
               current.Token is not null &&
               !current.Token.IsExpired(DateTimeOffset.UtcNow) &&
               !string.Equals(current.Token.Token, rejected.Token?.Token, StringComparison.Ordinal);
    }
}
