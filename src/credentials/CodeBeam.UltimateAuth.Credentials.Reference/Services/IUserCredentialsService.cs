using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public interface IUserCredentialsService
{
    Task<GetCredentialsResult> GetAllAsync(AccessContext context, CancellationToken ct = default);

    Task<AddCredentialResult> AddAsync(AccessContext context, AddCredentialRequest request, CancellationToken ct = default);

    Task<ChangeCredentialResult> ChangeAsync(AccessContext context, CredentialType type, ChangeCredentialRequest request, CancellationToken ct = default);

    Task<CredentialActionResult> RevokeAsync(AccessContext context, CredentialType type, RevokeCredentialRequest request, CancellationToken ct = default);

    Task<CredentialActionResult> ActivateAsync(AccessContext context, CredentialType type, CancellationToken ct = default);

    Task<CredentialActionResult> DeleteAsync(AccessContext context, CredentialType type, CancellationToken ct = default);
}
