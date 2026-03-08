using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface IUserClient
{
    Task<UAuthResult<UserCreateResult>> CreateAsync(CreateUserRequest request);
    Task<UAuthResult<UserStatusChangeResult>> ChangeStatusSelfAsync(ChangeUserStatusSelfRequest request);
    Task<UAuthResult<UserStatusChangeResult>> ChangeStatusAdminAsync(ChangeUserStatusAdminRequest request);
    Task<UAuthResult> DeleteMeAsync();
    Task<UAuthResult<UserDeleteResult>> DeleteAsync(DeleteUserRequest request);

    Task<UAuthResult<UserViewDto>> GetMeAsync();
    Task<UAuthResult> UpdateMeAsync(UpdateProfileRequest request);

    Task<UAuthResult<UserViewDto>> GetProfileAsync(UserKey userKey);
    Task<UAuthResult> UpdateProfileAsync(UserKey userKey, UpdateProfileRequest request);
}
