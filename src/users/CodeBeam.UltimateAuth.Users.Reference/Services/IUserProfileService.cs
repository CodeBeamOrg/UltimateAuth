using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public interface IUAuthUserProfileService
    {
        Task<UserProfileDto> GetCurrentAsync(AccessContext context, CancellationToken ct = default);
        Task UpdateCurrentAsync(AccessContext context, UpdateProfileRequest request, CancellationToken ct = default);
    }
}
