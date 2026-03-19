namespace CodeBeam.UltimateAuth.Client.Infrastructure;

public interface IUAuthClientBootstrapper
{
    Task EnsureStartedAsync(CancellationToken ct = default);
}
