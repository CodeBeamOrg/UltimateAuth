using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class SandboxAccountDeletionService(IDbContextFactory<LabDbContext> dbFactory, IUAuthClient client, ILogger<SandboxAccountDeletionService> logger)
{
    public async Task<bool> DeleteMemberAsync(
        UserKey authenticatedUserKey,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var membership = await db.SandboxMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.UserKey == authenticatedUserKey,
                ct);

        if (membership is null ||
            membership.Role != SandboxMemberRole.Member)
        {
            return false;
        }

        // This operation must execute as the authenticated user.
        var result = await client.Users.DeleteMeAsync();

        if (!result.IsSuccess)
            return false;

        var currentMembership = await db.SandboxMembers
            .SingleOrDefaultAsync(
                x => x.UserKey == authenticatedUserKey,
                ct);

        if (currentMembership is not null)
        {
            db.SandboxMembers.Remove(currentMembership);
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation(
            "Deleted member {UserKey} and removed sandbox membership.",
            authenticatedUserKey.Value);

        return true;
    }
}
