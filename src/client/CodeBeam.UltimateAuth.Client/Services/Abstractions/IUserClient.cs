using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface IUserClient
{
    Task<UAuthResult<PagedResult<UserSummary>>> QueryAsync(UserQuery query);
    Task<UAuthResult<UserCreateResult>> CreateAsync(CreateUserRequest request);
    Task<UAuthResult<UserCreateResult>> CreateAsAdminAsync(CreateUserRequest request);
    Task<UAuthResult<UserStatusChangeResult>> ChangeMyStatusAsync(ChangeUserStatusSelfRequest request);
    Task<UAuthResult<UserStatusChangeResult>> ChangeUserStatusAsync(UserKey userKey, ChangeUserStatusAdminRequest request);
    Task<UAuthResult> DeleteMeAsync();
    Task<UAuthResult<UserDeleteResult>> DeleteUserAsync(UserKey userKey, DeleteUserRequest request);

    Task<UAuthResult<UserView>> GetMeAsync();
    Task<UAuthResult> UpdateMeAsync(UpdateProfileRequest request);

    Task<UAuthResult<UserView>> GetUserAsync(UserKey userKey);
    Task<UAuthResult> UpdateUserAsync(UserKey userKey, UpdateProfileRequest request);
}
