using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference.Mapping;

namespace CodeBeam.UltimateAuth.Users.Reference.Services;

internal sealed class DefaultUserProfileService : IUAuthUserProfileService
{
    private readonly IUserProfileStore _profiles;
    private readonly ICurrentUser _currentUser;

    public DefaultUserProfileService(IUserProfileStore profiles, ICurrentUser currentUser)
    {
        _profiles = profiles;
        _currentUser = currentUser;
    }

    public async Task<UserProfileDto> GetCurrentAsync(string? tenantId, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException();

        var profile = await _profiles.GetAsync(tenantId, _currentUser.UserKey, ct) ?? throw new InvalidOperationException("User profile not found.");

        return UserProfileMapper.ToDto(profile);
    }

    public Task UpdateProfileAsync(string? tenantId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        return _profiles.UpdateAsync(tenantId, _currentUser.UserKey, request, ct);
    }
}
