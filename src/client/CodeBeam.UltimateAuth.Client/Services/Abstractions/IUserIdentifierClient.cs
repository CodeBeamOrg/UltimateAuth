using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface IUserIdentifierClient
{
    Task<UAuthResult<PagedResult<UserIdentifierInfo>>> GetMyIdentifiersAsync(PageRequest? request = null);
    Task<UAuthResult> AddSelfAsync(AddUserIdentifierRequest request);
    Task<UAuthResult> UpdateSelfAsync(UpdateUserIdentifierRequest request);
    Task<UAuthResult> SetPrimarySelfAsync(SetPrimaryUserIdentifierRequest request);
    Task<UAuthResult> UnsetPrimarySelfAsync(UnsetPrimaryUserIdentifierRequest request);
    Task<UAuthResult> VerifySelfAsync(VerifyUserIdentifierRequest request);
    Task<UAuthResult> DeleteSelfAsync(DeleteUserIdentifierRequest request);

    Task<UAuthResult<PagedResult<UserIdentifierInfo>>> GetUserIdentifiersAsync(UserKey userKey, PageRequest? request = null);
    Task<UAuthResult> AddAdminAsync(UserKey userKey, AddUserIdentifierRequest request);
    Task<UAuthResult> UpdateAdminAsync(UserKey userKey, UpdateUserIdentifierRequest request);
    Task<UAuthResult> SetPrimaryAdminAsync(UserKey userKey, SetPrimaryUserIdentifierRequest request);
    Task<UAuthResult> UnsetPrimaryAdminAsync(UserKey userKey, UnsetPrimaryUserIdentifierRequest request);
    Task<UAuthResult> VerifyAdminAsync(UserKey userKey, VerifyUserIdentifierRequest request);
    Task<UAuthResult> DeleteAdminAsync(UserKey userKey, DeleteUserIdentifierRequest request);
}
