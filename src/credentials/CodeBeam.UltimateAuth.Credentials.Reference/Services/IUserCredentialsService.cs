using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public interface IUserCredentialsService
{
    /// <summary>
    /// Sets the initial credential for a newly created user.
    /// Fails if a credential of the same type already exists.
    /// </summary>
    Task<CredentialProvisionResult> SetInitialAsync(string? tenantId, UserKey userKey, SetInitialCredentialRequest request, CancellationToken ct = default);
    Task<ChangeCredentialResult> ChangeAsync(string? tenantId, UserKey userKey, ChangeCredentialRequest request, CancellationToken ct = default);
    Task ResetAsync(string? tenantId, UserKey userKey, ResetPasswordRequest request, CancellationToken ct = default);
    Task RevokeAllAsync(string? tenantId, RevokeAllCredentialsRequest request, CancellationToken ct = default);
    Task DeleteAllAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);
}
