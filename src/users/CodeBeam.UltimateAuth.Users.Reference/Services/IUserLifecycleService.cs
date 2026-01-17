using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserLifecycleService
{
    Task<UserCreateResult> CreateAsync(string? tenantId, CreateUserRequest request, CancellationToken ct = default);
    Task<UserDeleteResult> DeleteAsync(string? tenantId, UserKey userKey, UserDeleteMode mode = UserDeleteMode.Soft, CancellationToken ct = default);
    Task<UserStatusChangeResult> ChangeStatusAsync(string? tenantId, UserKey userKey, UserStatus newStatus, CancellationToken ct = default);
}
