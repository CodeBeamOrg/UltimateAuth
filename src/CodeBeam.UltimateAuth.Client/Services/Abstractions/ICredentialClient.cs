using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface ICredentialClient
{
    Task<UAuthResult<AddCredentialResult>> AddMyAsync(AddCredentialRequest request);
    Task<UAuthResult<ChangeCredentialResult>> ChangeMyAsync(ChangeCredentialRequest request);
    Task<UAuthResult> RevokeMyAsync(RevokeCredentialRequest request);
    Task<UAuthResult<BeginCredentialResetResult>> BeginResetMyAsync(BeginCredentialResetRequest request);
    Task<UAuthResult<CredentialActionResult>> CompleteResetMyAsync(CompleteCredentialResetRequest request);

    Task<UAuthResult<AddCredentialResult>> AddCredentialAsync(UserKey userKey, AddCredentialRequest request);
    Task<UAuthResult<ChangeCredentialResult>> ChangeCredentialAsync(UserKey userKey, ChangeCredentialRequest request);
    Task<UAuthResult> RevokeCredentialAsync(UserKey userKey, RevokeCredentialRequest request);
    Task<UAuthResult<BeginCredentialResetResult>> BeginResetCredentialAsync(UserKey userKey, BeginCredentialResetRequest request);
    Task<UAuthResult<CredentialActionResult>> CompleteResetCredentialAsync(UserKey userKey, CompleteCredentialResetRequest request);
    Task<UAuthResult> DeleteCredentialAsync(UserKey userKey);
}
