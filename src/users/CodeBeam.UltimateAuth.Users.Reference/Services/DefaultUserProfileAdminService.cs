using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class DefaultUserProfileAdminService : IUserProfileAdminService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly IUserProfileStore _profiles;

    public DefaultUserProfileAdminService(IAccessOrchestrator accessOrchestrator, IUserProfileStore profiles)
    {
        _accessOrchestrator = accessOrchestrator;
        _profiles = profiles;
    }

    public async Task<UserProfileDto> GetAsync(AccessContext context, UserKey targetUserKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var policies = Array.Empty<IAccessPolicy>();

        var cmd = new GetUserProfileAdminCommand(
            async innerCt =>
            {
                var profile = await _profiles.GetAsync(context.ResourceTenantId, targetUserKey, innerCt);

                if (profile is null)
                    throw new InvalidOperationException("user_profile_not_found");

                return UserProfileMapper.ToDto(profile);
            });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task UpdateAsync(AccessContext context, UserKey targetUserKey, UpdateProfileRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var policies = Array.Empty<IAccessPolicy>();

        var cmd = new UpdateUserProfileAdminCommand(
            async innerCt =>
            {
                await _profiles.UpdateAsync(context.ResourceTenantId, targetUserKey, request, innerCt);
            });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }
}
