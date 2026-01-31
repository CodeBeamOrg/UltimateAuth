using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public interface IUserApplicationService
    {
        Task<UserViewDto> GetMeAsync(AccessContext context, CancellationToken ct = default);
        Task<UserViewDto> GetUserProfileAsync(AccessContext context, CancellationToken ct = default);

        Task<UserCreateResult> CreateUserAsync(AccessContext context, CreateUserRequest request, CancellationToken ct = default);

        Task ChangeUserStatusAsync(AccessContext context, object request, CancellationToken ct = default);

        Task UpdateUserProfileAsync(AccessContext context, UpdateProfileRequest request, CancellationToken ct = default);

        Task<IReadOnlyList<UserIdentifierDto>> GetIdentifiersByUserAsync(AccessContext context, CancellationToken ct = default);

        Task<UserIdentifierDto?> GetIdentifierAsync(AccessContext context, UserIdentifierType type, string value, CancellationToken ct = default);

        Task<bool> UserIdentifierExistsAsync(AccessContext context, UserIdentifierType type, string value, CancellationToken ct = default);

        Task AddUserIdentifierAsync(AccessContext context, AddUserIdentifierRequest request, CancellationToken ct = default);

        Task UpdateUserIdentifierAsync(AccessContext context, UpdateUserIdentifierRequest request, CancellationToken ct = default);

        Task SetPrimaryUserIdentifierAsync(AccessContext context, SetPrimaryUserIdentifierRequest request, CancellationToken ct = default);

        Task UnsetPrimaryUserIdentifierAsync(AccessContext context, UnsetPrimaryUserIdentifierRequest request, CancellationToken ct = default);

        Task VerifyUserIdentifierAsync(AccessContext context, VerifyUserIdentifierRequest request, CancellationToken ct = default);

        Task DeleteUserIdentifierAsync(AccessContext context, DeleteUserIdentifierRequest request, CancellationToken ct = default);

        Task DeleteUserAsync(AccessContext context, DeleteUserRequest request, CancellationToken ct = default);
    }
}
