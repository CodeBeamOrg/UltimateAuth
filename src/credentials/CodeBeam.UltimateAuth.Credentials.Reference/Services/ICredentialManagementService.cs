using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public interface ICredentialManagementService
{
    Task<GetCredentialsResult> GetAllAsync(AccessContext context, CancellationToken ct = default);

    Task<AddCredentialResult> AddAsync(AccessContext context, AddCredentialRequest request, CancellationToken ct = default);

    Task<ChangeCredentialResult> ChangeSecretAsync(AccessContext context, ChangeCredentialRequest request, CancellationToken ct = default);

    Task<CredentialActionResult> RevokeAsync(AccessContext context, RevokeCredentialRequest request, CancellationToken ct = default);

    Task<CredentialActionResult> BeginResetAsync(AccessContext context, BeginCredentialResetRequest request, CancellationToken ct = default);

    Task<CredentialActionResult> CompleteResetAsync(AccessContext context, CompleteCredentialResetRequest request, CancellationToken ct = default);

    Task<CredentialActionResult> DeleteAsync(AccessContext context, DeleteCredentialRequest request, CancellationToken ct = default);
}
