using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public interface IUserApplicationService
{
    Task<UserView> GetMeAsync(AccessContext context, CancellationToken ct = default);
    Task<UserView> GetUserProfileAsync(AccessContext context, CancellationToken ct = default);

    Task<PagedResult<UserSummary>> QueryUsersAsync(AccessContext context, UserQuery query, CancellationToken ct = default);
    Task<UserCreateResult> CreateUserAsync(AccessContext context, CreateUserRequest request, CancellationToken ct = default);

    Task ChangeUserStatusAsync(AccessContext context, object request, CancellationToken ct = default);

    Task UpdateUserProfileAsync(AccessContext context, UpdateProfileRequest request, CancellationToken ct = default);

    Task<PagedResult<UserIdentifierInfo>> GetIdentifiersByUserAsync(AccessContext context, UserIdentifierQuery query, CancellationToken ct = default);

    Task<UserIdentifierInfo?> GetIdentifierAsync(AccessContext context, UserIdentifierType type, string value, CancellationToken ct = default);

    Task<bool> UserIdentifierExistsAsync(AccessContext context, UserIdentifierType type, string value, IdentifierExistenceScope scope = IdentifierExistenceScope.TenantPrimaryOnly, CancellationToken ct = default);

    Task AddUserIdentifierAsync(AccessContext context, AddUserIdentifierRequest request, CancellationToken ct = default);

    Task UpdateUserIdentifierAsync(AccessContext context, UpdateUserIdentifierRequest request, CancellationToken ct = default);

    Task SetPrimaryUserIdentifierAsync(AccessContext context, SetPrimaryUserIdentifierRequest request, CancellationToken ct = default);

    Task UnsetPrimaryUserIdentifierAsync(AccessContext context, UnsetPrimaryUserIdentifierRequest request, CancellationToken ct = default);

    Task VerifyUserIdentifierAsync(AccessContext context, VerifyUserIdentifierRequest request, CancellationToken ct = default);

    Task DeleteUserIdentifierAsync(AccessContext context, DeleteUserIdentifierRequest request, CancellationToken ct = default);

    Task DeleteMeAsync(AccessContext context, CancellationToken ct = default);
    Task DeleteUserAsync(AccessContext context, DeleteUserRequest request, CancellationToken ct = default);
}
