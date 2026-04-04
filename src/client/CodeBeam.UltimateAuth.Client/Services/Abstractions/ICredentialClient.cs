using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface ICredentialClient
{
    Task<UAuthResult<AddCredentialResult>> AddMyAsync(AddCredentialRequest request);
    Task<UAuthResult<ChangeCredentialResult>> ChangeMyAsync(ChangeCredentialRequest request);
    Task<UAuthResult> RevokeMyAsync(RevokeCredentialRequest request);
    Task<UAuthResult<BeginCredentialResetResult>> BeginResetMyAsync(BeginResetCredentialRequest request);
    Task<UAuthResult<CredentialActionResult>> CompleteResetMyAsync(CompleteResetCredentialRequest request);

    Task<UAuthResult<AddCredentialResult>> AddUserAsync(UserKey userKey, AddCredentialRequest request);
    Task<UAuthResult<ChangeCredentialResult>> ChangeUserAsync(UserKey userKey, ChangeCredentialRequest request);
    Task<UAuthResult> RevokeUserAsync(UserKey userKey, RevokeCredentialRequest request);
    Task<UAuthResult<BeginCredentialResetResult>> BeginResetUserAsync(UserKey userKey, BeginResetCredentialRequest request);
    Task<UAuthResult<CredentialActionResult>> CompleteResetUserAsync(UserKey userKey, CompleteResetCredentialRequest request);
    Task<UAuthResult> DeleteUserAsync(UserKey userKey, DeleteCredentialRequest request);
}
