using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface ICredentialClient
{
    Task<UAuthResult<GetCredentialsResult>> GetMyAsync();
    Task<UAuthResult<AddCredentialResult>> AddMyAsync(AddCredentialRequest request);
    Task<UAuthResult<ChangeCredentialResult>> ChangeMyAsync(ChangeCredentialRequest request);
    Task<UAuthResult> RevokeMyAsync(RevokeCredentialRequest request);
    Task<UAuthResult> BeginResetMyAsync(BeginCredentialResetRequest request);
    Task<UAuthResult> CompleteResetMyAsync(CompleteCredentialResetRequest request);

    Task<UAuthResult<GetCredentialsResult>> GetUserAsync(UserKey userKey);
    Task<UAuthResult<AddCredentialResult>> AddUserAsync(UserKey userKey, AddCredentialRequest request);
    Task<UAuthResult> RevokeUserAsync(UserKey userKey, RevokeCredentialRequest request);
    Task<UAuthResult> ActivateUserAsync(UserKey userKey);
    Task<UAuthResult> BeginResetUserAsync(UserKey userKey, BeginCredentialResetRequest request);
    Task<UAuthResult> CompleteResetUserAsync(UserKey userKey, CompleteCredentialResetRequest request);
    Task<UAuthResult> DeleteUserAsync(UserKey userKey);
}
