namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Contributes seed data for a specific domain (Users, Credentials, Authorization, etc).
/// Intended for dev/test/in-memory environments.
/// </summary>
public interface ISeedContributor
{
    /// <summary>
    /// Execution order relative to other contributors.
    /// Lower numbers run first.
    /// </summary>
    int Order { get; }

    Task SeedAsync(string? tenantId, CancellationToken ct = default);
}
