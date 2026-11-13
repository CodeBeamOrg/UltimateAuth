using CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class UAuthPasswordHashLimiterTests
{
    private static UAuthPasswordHashLimiter Create(
        int global = 2,
        int login = 2,
        int credential = 2,
        int? queue = 0,
        TimeSpan? timeout = null,
        bool enabled = true)
    {
        var options = new UAuthServerOptions();
        var concurrency = options.ConcurrencyLimiting;

        concurrency.Enabled = enabled;

        foreach (var rule in new[]
        {
            concurrency.PasswordHashing,
            concurrency.Login,
            concurrency.CredentialManagement
        })
        {
            rule.QueueLimit = queue;
            rule.QueueTimeout =
                timeout ?? TimeSpan.FromSeconds(3);
        }

        concurrency.PasswordHashing.PermitLimit = global;
        concurrency.Login.PermitLimit = login;
        concurrency.CredentialManagement.PermitLimit = credential;

        return new UAuthPasswordHashLimiter(
            Options.Create(options));
    }

    private static TaskCompletionSource<bool> Gate() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    [Fact]
    public async Task Login_RespectsWorkloadPermitLimit()
    {
        using var limiter = Create(global: 3, login: 1);
        var gate = Gate();
        var started = Gate();

        var first = limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            async () =>
            {
                started.TrySetResult(true);
                return await gate.Task;
            });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        try
        {
            await Assert.ThrowsAsync<PasswordVerificationCapacityException>(
                () => limiter.ExecuteAsync(
                    UAuthPasswordHashWorkload.Login,
                    () => Task.FromResult(true)));
        }
        finally
        {
            gate.TrySetResult(true);
            await first;
        }
    }

    [Fact]
    public async Task DifferentWorkloads_ShareGlobalCapacity()
    {
        using var limiter = Create(global: 1, login: 2, credential: 2);
        var gate = Gate();
        var started = Gate();

        var first = limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            async () =>
            {
                started.TrySetResult(true);
                return await gate.Task;
            });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        try
        {
            await Assert.ThrowsAsync<PasswordVerificationCapacityException>(
                () => limiter.ExecuteAsync(
                    UAuthPasswordHashWorkload.CredentialManagement,
                    () => Task.FromResult(true)));
        }
        finally
        {
            gate.TrySetResult(true);
            await first;
        }
    }

    [Fact]
    public async Task AvailableGlobalCapacity_CanServeOtherWorkloads()
    {
        using var limiter = Create(global: 2, login: 1, credential: 1);
        var gate = Gate();
        var started = Gate();

        var login = limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            async () =>
            {
                started.TrySetResult(true);
                return await gate.Task;
            });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        try
        {
            var result = await limiter.ExecuteAsync(
                UAuthPasswordHashWorkload.CredentialManagement,
                () => Task.FromResult(42));

            Assert.Equal(42, result);
        }
        finally
        {
            gate.TrySetResult(true);
            await login;
        }
    }

    [Fact]
    public async Task Queue_AllowsWaitingRequestToProceed()
    {
        using var limiter = Create(
            global: 1, login: 1, queue: 1);

        var gate = Gate();
        var started = Gate();

        var first = limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            async () =>
            {
                started.TrySetResult(true);
                return await gate.Task;
            });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var secondStarted = Gate();

        var second = limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            () =>
            {
                secondStarted.TrySetResult(true);
                return Task.FromResult(true);
            });

        Assert.False(secondStarted.Task.IsCompleted);

        gate.TrySetResult(true);

        Assert.True(await first);
        Assert.True(await second.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(secondStarted.Task.IsCompleted);
    }

    [Fact]
    public async Task QueueTimeout_RejectsWaitingOperation()
    {
        using var limiter = Create(
            global: 1,
            login: 1,
            queue: 1,
            timeout: TimeSpan.FromMilliseconds(100));

        var gate = Gate();
        var started = Gate();

        var first = limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            async () =>
            {
                started.TrySetResult(true);
                return await gate.Task;
            });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        try
        {
            await Assert.ThrowsAsync<PasswordVerificationCapacityException>(
                () => limiter.ExecuteAsync(
                    UAuthPasswordHashWorkload.Login,
                    () => Task.FromResult(true)));
        }
        finally
        {
            gate.TrySetResult(true);
            await first;
        }
    }

    [Fact]
    public async Task RequestCancellation_IsNotConvertedToCapacityError()
    {
        using var limiter = Create(
            global: 1, login: 1, queue: 1);

        var gate = Gate();
        var started = Gate();

        var first = limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            async () =>
            {
                started.TrySetResult(true);
                return await gate.Task;
            });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        using var cancellation = new CancellationTokenSource();

        var waiting = limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            () => Task.FromResult(true),
            cancellation.Token);

        try
        {
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => waiting);
        }
        finally
        {
            gate.TrySetResult(true);
            await first;
        }
    }

    [Fact]
    public async Task FailedOperation_ReleasesBothPermits()
    {
        using var limiter = Create(global: 1, login: 1);

        for (var i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => limiter.ExecuteAsync(
                    UAuthPasswordHashWorkload.Login,
                    () => Task.FromException<bool>(
                        new InvalidOperationException("failure"))));
        }

        var result = await limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            () => Task.FromResult(true));

        Assert.True(result);
    }

    [Fact]
    public async Task DisabledLimiting_AllowsConcurrentOperations()
    {
        using var limiter = Create(
            global: 1, login: 1, queue: 0, enabled: false);

        var gate = Gate();
        var started = Gate();

        var first = limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            async () =>
            {
                started.TrySetResult(true);
                return await gate.Task;
            });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        try
        {
            var second = await limiter.ExecuteAsync(
                UAuthPasswordHashWorkload.Login,
                () => Task.FromResult(true));

            Assert.True(second);
        }
        finally
        {
            gate.TrySetResult(true);
            await first;
        }
    }

    [Fact]
    public async Task NullQueueLimit_AllowsWaitingWithoutFixedQueueCap()
    {
        using var limiter = Create(
            global: 1,
            login: 1,
            queue: null);

        var gate = Gate();
        var started = Gate();

        var first = limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            async () =>
            {
                started.TrySetResult(true);
                return await gate.Task;
            });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var waiting = Enumerable.Range(0, 5)
            .Select(_ => limiter.ExecuteAsync(
                UAuthPasswordHashWorkload.Login,
                () => Task.FromResult(true)))
            .ToArray();

        try
        {
            gate.TrySetResult(true);
            await first;

            var results = await Task.WhenAll(waiting)
                .WaitAsync(TimeSpan.FromSeconds(5));

            Assert.All(results, Assert.True);
        }
        finally
        {
            gate.TrySetResult(true);
        }
    }


    [Fact]
    public async Task FullQueue_RejectsAdditionalRequest()
    {
        using var limiter = Create(
            global: 1,
            login: 1,
            queue: 1);

        var gate = Gate();
        var started = Gate();

        var first = limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            async () =>
            {
                started.TrySetResult(true);
                return await gate.Task;
            });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var second = limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            () => Task.FromResult(true));

        try
        {
            await Assert.ThrowsAsync<PasswordVerificationCapacityException>(
                () => limiter.ExecuteAsync(
                    UAuthPasswordHashWorkload.Login,
                    () => Task.FromResult(true)));
        }
        finally
        {
            gate.TrySetResult(true);
            await first;
            await second;
        }
    }

    [Fact]
    public async Task GlobalTimeout_ReleasesWorkloadPermit()
    {
        using var limiter = Create(
            global: 1,
            login: 1,
            credential: 1,
            queue: 1,
            timeout: TimeSpan.FromMilliseconds(100));

        var gate = Gate();
        var started = Gate();

        var credentialOperation = limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.CredentialManagement,
            async () =>
            {
                started.TrySetResult(true);
                return await gate.Task;
            });

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        try
        {
            // Obtains the Login permit, then waits for global capacity.
            await Assert.ThrowsAsync<PasswordVerificationCapacityException>(
                () => limiter.ExecuteAsync(
                    UAuthPasswordHashWorkload.Login,
                    () => Task.FromResult(true)));
        }
        finally
        {
            gate.TrySetResult(true);
            await credentialOperation;
        }

        // A failed global acquisition must not leak the Login permit.
        var result = await limiter.ExecuteAsync(
            UAuthPasswordHashWorkload.Login,
            () => Task.FromResult(true));

        Assert.True(result);
    }
}
