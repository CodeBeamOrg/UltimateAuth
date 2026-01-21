using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public interface IUserProfileAdminService
    {
        Task<UserProfileDto> GetAsync(AccessContext context, UserKey targetUserKey, CancellationToken ct = default);
        Task UpdateAsync(AccessContext context, UserKey targetUserKey, UpdateProfileRequest request, CancellationToken ct = default);
    }
}
