namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface IAccessCommand
    {
        Task ExecuteAsync(CancellationToken ct = default);
    }

    // For get commands
    public interface IAccessCommand<TResult>
    {
        Task<TResult> ExecuteAsync(CancellationToken ct = default);
    }

}
