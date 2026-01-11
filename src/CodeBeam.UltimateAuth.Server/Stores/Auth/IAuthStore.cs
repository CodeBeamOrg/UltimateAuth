using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Stores;

public interface IAuthStore
{
    Task StoreAsync(AuthArtifactKey key, AuthArtifact artifact, CancellationToken cancellationToken = default);

    Task<AuthArtifact?> GetAsync(AuthArtifactKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically gets and removes the artifact.
    /// This MUST be consume-once.
    /// </summary>
    Task<AuthArtifact?> ConsumeAsync(AuthArtifactKey key, CancellationToken cancellationToken = default);
}
