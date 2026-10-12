using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public interface ICredentialManagementService
{
    Task<CredentialsGetResult> GetAllAsync(AccessContext context, CancellationToken ct = default);

    Task<CredentialAddResult> AddAsync(AccessContext context, AddCredentialRequest request, CancellationToken ct = default);

    Task<CredentialChangeResult> ChangeSecretAsync(AccessContext context, ChangeCredentialRequest request, CancellationToken ct = default);

    Task<CredentialActionResult> RevokeAsync(AccessContext context, RevokeCredentialRequest request, CancellationToken ct = default);

    Task<CredentialBeginResetResult> BeginResetAsync(AccessContext context, BeginResetCredentialRequest request, CancellationToken ct = default);

    Task<CredentialActionResult> CompleteResetAsync(AccessContext context, CompleteResetCredentialRequest request, CancellationToken ct = default);

    Task<CredentialActionResult> DeleteAsync(AccessContext context, DeleteCredentialRequest request, CancellationToken ct = default);
}
