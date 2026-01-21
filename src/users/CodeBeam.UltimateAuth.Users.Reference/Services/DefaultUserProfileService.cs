using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class DefaultUserProfileService : IUAuthUserProfileService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly IUserProfileStore _profiles;

    public DefaultUserProfileService(IAccessOrchestrator accessOrchestrator, IUserProfileStore profiles)
    {
        _accessOrchestrator = accessOrchestrator;
        _profiles = profiles;
    }

    public async Task<UserProfileDto> GetCurrentAsync(AccessContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var policies = Array.Empty<IAccessPolicy>();

        var cmd = new GetCurrentUserProfileCommand(policies,
            async innerCt =>
            {
                if (context.ActorUserKey is null)
                    throw new UnauthorizedAccessException();

                var profile = await _profiles.GetAsync(context.ResourceTenantId, (UserKey)context.ActorUserKey, innerCt);

                if (profile is null)
                    throw new InvalidOperationException("user_profile_not_found");

                return UserProfileMapper.ToDto(profile);
            });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task UpdateCurrentAsync(AccessContext context, UpdateProfileRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var policies = Array.Empty<IAccessPolicy>();

        var cmd = new UpdateCurrentUserProfileCommand(policies,
            async innerCt =>
            {
                if (context.ActorUserKey is null)
                    throw new UnauthorizedAccessException();

                await _profiles.UpdateAsync(context.ResourceTenantId, (UserKey)context.ActorUserKey, request, innerCt);
            });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }
}
