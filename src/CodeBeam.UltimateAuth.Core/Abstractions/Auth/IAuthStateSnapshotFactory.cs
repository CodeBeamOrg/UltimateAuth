using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IAuthStateSnapshotFactory
{
    Task<AuthStateSnapshot?> CreateAsync(SessionValidationResult validation, CancellationToken ct = default);
}
