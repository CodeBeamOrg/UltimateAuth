namespace CodeBeam.UltimateAuth.Client.Infrastructure;

/// <summary>
/// Represents a bootstrapper for the UltimateAuth client, responsible for ensuring that the client is properly initialized and started before use.
/// </summary>
public interface IUAuthClientBootstrapper
{
    /// <summary>
    /// Ensures that the UltimateAuth client is started and ready for use. This method should be called before any operations that require the client to be initialized.
    /// </summary>
    Task EnsureStartedAsync(CancellationToken ct = default);
}
