using System.Threading.RateLimiting;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;

internal sealed class UAuthPasswordHashLimiter : IUAuthPasswordHashWorkLimiter, IDisposable
{
    private readonly ConcurrencyLimiter? _limiter;
    private readonly TimeSpan _timeout;

    public UAuthPasswordHashLimiter(IOptions<UAuthServerOptions> serverOptions)
    {
        var options = serverOptions.Value.ConcurrencyLimiting;

        if (!options.Enabled)
            return;

        var rule = options.PasswordHashing;

        _timeout = rule.QueueTimeout;

        _limiter = new ConcurrencyLimiter(
            new ConcurrencyLimiterOptions
            {
                PermitLimit = rule.PermitLimit,
                QueueLimit = rule.EffectiveQueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ct.ThrowIfCancellationRequested();

        if (_limiter is null)
            return await operation();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);
        RateLimitLease lease;

        try
        {
            lease = await _limiter.AcquireAsync(permitCount: 1, cancellationToken: timeoutCts.Token);
        }
        catch (OperationCanceledException)
            when (!ct.IsCancellationRequested)
        {
            throw new PasswordVerificationCapacityException();
        }

        using (lease)
        {
            if (!lease.IsAcquired)
                throw new PasswordVerificationCapacityException();

            ct.ThrowIfCancellationRequested();

            // Only the wait is timed out.
            // The acquired operation is not interrupted.
            return await operation();
        }
    }

    public Task ExecuteAsync(Action operation, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return ExecuteAsync(
            () =>
            {
                operation();
                return Task.FromResult(true);
            },
            ct);
    }

    public void Dispose() => _limiter?.Dispose();
}
