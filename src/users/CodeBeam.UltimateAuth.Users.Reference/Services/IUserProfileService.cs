using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public interface IUAuthUserProfileService
    {
        Task<UserProfileDto> GetCurrentAsync(string? tenantId, CancellationToken ct = default);
        Task UpdateProfileAsync(string? tenantId, UpdateProfileRequest request, CancellationToken ct = default);
    }
}
