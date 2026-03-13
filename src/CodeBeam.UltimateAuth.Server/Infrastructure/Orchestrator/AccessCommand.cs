namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class AccessCommand : IAccessCommand
{
    private readonly Func<CancellationToken, Task> _execute;

    public AccessCommand(Func<CancellationToken, Task> execute)
    {
        _execute = execute;
    }

    public Task ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}

public sealed class AccessCommand<TResult> : IAccessCommand<TResult>
{
    private readonly Func<CancellationToken, Task<TResult>> _execute;

    public AccessCommand(Func<CancellationToken, Task<TResult>> execute)
    {
        _execute = execute;
    }

    public Task<TResult> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
