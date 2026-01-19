using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserLifecycleService
{
    Task<UserCreateResult> CreateAsync(string? tenantId, CreateUserRequest request, CancellationToken ct = default);
    Task<UserDeleteResult> DeleteAsync(string? tenantId, DeleteUserRequest request, CancellationToken ct = default);
    Task<UserStatusChangeResult> ChangeStatusAsync(string? tenantId, ChangeUserStatusRequest request, CancellationToken ct = default);
}
