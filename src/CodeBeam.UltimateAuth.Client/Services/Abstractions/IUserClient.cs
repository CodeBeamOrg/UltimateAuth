using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface IUserClient
{
    Task<UAuthResult<PagedResult<UserSummary>>> QueryUsersAsync(UserQuery query);
    Task<UAuthResult<UserCreateResult>> CreateAsync(CreateUserRequest request);
    Task<UAuthResult<UserCreateResult>> CreateAdminAsync(CreateUserRequest request);
    Task<UAuthResult<UserStatusChangeResult>> ChangeStatusSelfAsync(ChangeUserStatusSelfRequest request);
    Task<UAuthResult<UserStatusChangeResult>> ChangeStatusAdminAsync(UserKey userKey, ChangeUserStatusAdminRequest request);
    Task<UAuthResult> DeleteMeAsync();
    Task<UAuthResult<UserDeleteResult>> DeleteUserAsync(UserKey userKey, DeleteUserRequest request);

    Task<UAuthResult<UserView>> GetMeAsync();
    Task<UAuthResult> UpdateMeAsync(UpdateProfileRequest request);

    Task<UAuthResult<UserView>> GetProfileAsync(UserKey userKey);
    Task<UAuthResult> UpdateProfileAsync(UserKey userKey, UpdateProfileRequest request);
}
