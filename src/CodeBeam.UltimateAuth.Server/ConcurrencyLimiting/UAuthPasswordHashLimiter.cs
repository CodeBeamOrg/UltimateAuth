using System.Threading.RateLimiting;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;

internal sealed class UAuthPasswordHashLimiter : IUAuthPasswordHashWorkLimiter, IDisposable
{
    private readonly ConcurrencyLimiter? _global;
    private readonly ConcurrencyLimiter? _login;
    private readonly ConcurrencyLimiter? _credentialManagement;

    private readonly TimeSpan _globalTimeout;
    private readonly TimeSpan _loginTimeout;
    private readonly TimeSpan _credentialTimeout;

    public UAuthPasswordHashLimiter(IOptions<UAuthServerOptions> serverOptions)
    {
        var options = serverOptions.Value.ConcurrencyLimiting;

        if (!options.Enabled)
            return;

        _global = CreateLimiter(options.PasswordHashing);
        _login = CreateLimiter(options.Login);
        _credentialManagement = CreateLimiter(options.CredentialManagement);

        _globalTimeout = options.PasswordHashing.QueueTimeout;
        _loginTimeout = options.Login.QueueTimeout;
        _credentialTimeout = options.CredentialManagement.QueueTimeout;
    }

    public async Task<T> ExecuteAsync<T>(
        UAuthPasswordHashWorkload workload,
        Func<Task<T>> operation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ct.ThrowIfCancellationRequested();

        if (_global is null)
            return await operation();

        var (localLimiter, localTimeout) = workload switch
        {
            UAuthPasswordHashWorkload.Login => (_login!, _loginTimeout),
            UAuthPasswordHashWorkload.CredentialManagement => (_credentialManagement!, _credentialTimeout),
            _ => throw new ArgumentOutOfRangeException(nameof(workload))
        };

        var timeout = _globalTimeout <= localTimeout
            ? _globalTimeout
            : localTimeout;

        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(ct);

        timeoutCts.CancelAfter(timeout);

        using var localLease = await AcquireAsync(
            localLimiter, ct, timeoutCts.Token);

        using var globalLease = await AcquireAsync(
            _global, ct, timeoutCts.Token);

        ct.ThrowIfCancellationRequested();

        return await operation();
    }

    public Task ExecuteAsync(
        UAuthPasswordHashWorkload workload,
        Action operation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return ExecuteAsync(
            workload,
            () =>
            {
                operation();
                return Task.FromResult(true);
            },
            ct);
    }

    private static ConcurrencyLimiter CreateLimiter(
        UAuthConcurrencyRule rule)
    {
        return new ConcurrencyLimiter(
            new ConcurrencyLimiterOptions
            {
                PermitLimit = rule.PermitLimit,

                // Public null means effectively unbounded. The .NET limiter requires an integer limit.
                QueueLimit = rule.QueueLimit ?? int.MaxValue,

                QueueProcessingOrder =
                    QueueProcessingOrder.OldestFirst
            });
    }

    private static async Task<RateLimitLease> AcquireAsync(
        ConcurrencyLimiter limiter,
        CancellationToken requestCt,
        CancellationToken waitCt)
    {
        RateLimitLease lease;

        try
        {
            lease = await limiter.AcquireAsync(
                permitCount: 1,
                cancellationToken: waitCt);
        }
        catch (OperationCanceledException)
            when (!requestCt.IsCancellationRequested)
        {
            throw new PasswordVerificationCapacityException();
        }

        if (!lease.IsAcquired)
        {
            lease.Dispose();
            throw new PasswordVerificationCapacityException();
        }

        return lease;
    }

    public void Dispose()
    {
        _credentialManagement?.Dispose();
        _login?.Dispose();
        _global?.Dispose();
    }
}
