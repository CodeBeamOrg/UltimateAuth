using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface ICredentialClient
{
    Task<UAuthResult<GetCredentialsResult>> GetMyAsync();
    Task<UAuthResult<AddCredentialResult>> AddMyAsync(AddCredentialRequest request);
    Task<UAuthResult<ChangeCredentialResult>> ChangeMyAsync(CredentialType type, ChangeCredentialRequest request);
    Task<UAuthResult> RevokeMyAsync(CredentialType type, RevokeCredentialRequest request);
    Task<UAuthResult> BeginResetMyAsync(CredentialType type, BeginCredentialResetRequest request);
    Task<UAuthResult> CompleteResetMyAsync(CredentialType type, CompleteCredentialResetRequest request);

    Task<UAuthResult<GetCredentialsResult>> GetUserAsync(UserKey userKey);
    Task<UAuthResult<AddCredentialResult>> AddUserAsync(UserKey userKey, AddCredentialRequest request);
    Task<UAuthResult> RevokeUserAsync(UserKey userKey, CredentialType type, RevokeCredentialRequest request);
    Task<UAuthResult> ActivateUserAsync(UserKey userKey, CredentialType type);
    Task<UAuthResult> BeginResetUserAsync(UserKey userKey, CredentialType type, BeginCredentialResetRequest request);
    Task<UAuthResult> CompleteResetUserAsync(UserKey userKey, CredentialType type, CompleteCredentialResetRequest request);
    Task<UAuthResult> DeleteUserAsync(UserKey userKey, CredentialType type);
}
