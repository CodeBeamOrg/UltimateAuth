namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface IAccessCommand
    {
        Task ExecuteAsync(CancellationToken ct = default);
    }

}
