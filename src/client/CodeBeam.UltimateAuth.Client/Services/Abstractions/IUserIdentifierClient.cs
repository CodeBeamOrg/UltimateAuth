using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface IUserIdentifierClient
{
    Task<UAuthResult<PagedResult<UserIdentifierInfo>>> GetMyAsync(PageRequest? request = null);
    Task<UAuthResult> AddMyAsync(AddUserIdentifierRequest request);
    Task<UAuthResult> UpdateMyAsync(UpdateUserIdentifierRequest request);
    Task<UAuthResult> SetMyPrimaryAsync(SetPrimaryUserIdentifierRequest request);
    Task<UAuthResult> UnsetMyPrimaryAsync(UnsetPrimaryUserIdentifierRequest request);
    Task<UAuthResult> VerifyMyAsync(VerifyUserIdentifierRequest request);
    Task<UAuthResult> DeleteMyAsync(DeleteUserIdentifierRequest request);

    Task<UAuthResult<PagedResult<UserIdentifierInfo>>> GetUserAsync(UserKey userKey, PageRequest? request = null);
    Task<UAuthResult> AddUserAsync(UserKey userKey, AddUserIdentifierRequest request);
    Task<UAuthResult> UpdateUserAsync(UserKey userKey, UpdateUserIdentifierRequest request);
    Task<UAuthResult> SetUserPrimaryAsync(UserKey userKey, SetPrimaryUserIdentifierRequest request);
    Task<UAuthResult> UnsetUserPrimaryAsync(UserKey userKey, UnsetPrimaryUserIdentifierRequest request);
    Task<UAuthResult> VerifyUserAsync(UserKey userKey, VerifyUserIdentifierRequest request);
    Task<UAuthResult> DeleteUserAsync(UserKey userKey, DeleteUserIdentifierRequest request);
}
