using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Options;
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

        if (!_options.Proactive || snapshot.Token is null)
        {
            return snapshot;
        }

        var now = DateTimeOffset.UtcNow;

        if (snapshot.Token.ExpiresAt - now > _options.RefreshBeforeExpiration)
        {
            return snapshot;
        }

        await _coordinator.RefreshAsync(UAuthRefreshTrigger.Proactive, ct);

        return _tokenStore.Capture();
    }

    public async Task<UAuthTokenSnapshot?> RecoverAsync(UAuthTokenSnapshot rejected, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_options.Reactive || rejected.Token is null)
        {
            return null;
        }

        var current = _tokenStore.Capture();

        // Never replay a request under another authentication epoch.
        if (current.Epoch != rejected.Epoch)
        {
            return null;
        }

        // Another request may already have refreshed the token.
        if (current.Generation != rejected.Generation)
        {
            return CanReplay(rejected, current)
                ? current
                : null;
        }

        var result = await _coordinator.RefreshAsync(UAuthRefreshTrigger.Reactive, ct);

        if (!result.IsSuccess)
        {
            return null;
        }

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
