using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserLifecycleService
{
    Task<UserCreateResult> CreateAsync(AccessContext context, CreateUserRequest request, CancellationToken ct = default);
    Task<UserStatusChangeResult> ChangeStatusAsync(AccessContext context, ChangeUserStatusRequest request, CancellationToken ct = default);
    Task<UserDeleteResult> DeleteAsync(AccessContext context, DeleteUserRequest request, CancellationToken ct = default);
}
