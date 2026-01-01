namespace CodeBeam.UltimateAuth.Client.Abstractions
{
    public interface ISessionCoordinator : IAsyncDisposable
    {
        /// <summary>
        /// Starts session coordination.
        /// Should be idempotent.
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops coordination (optional).
        /// </summary>
        Task StopAsync();

        event Action? ReauthRequired;
    }
}
