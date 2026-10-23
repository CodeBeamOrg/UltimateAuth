using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class SandboxAccessService(IDbContextFactory<LabDbContext> dbFactory)
{
    public Task<SandboxAccessResult> CanAccessAsync(UserKey userKey, Guid sandboxId, CancellationToken cancellationToken = default)
        => CheckAsync(userKey, sandboxId, AccessRequirement.Access, cancellationToken);

    public Task<SandboxAccessResult> CanReadAsync(UserKey userKey, Guid sandboxId, CancellationToken cancellationToken = default)
        => CheckAsync(userKey, sandboxId, AccessRequirement.Read, cancellationToken);

    public Task<SandboxAccessResult> CanWriteAsync(UserKey userKey, Guid sandboxId, CancellationToken cancellationToken = default)
        => CheckAsync(userKey, sandboxId, AccessRequirement.Write, cancellationToken);

    public Task<SandboxAccessResult> CanManageAsync(UserKey userKey, Guid sandboxId, CancellationToken cancellationToken = default)
        => CheckAsync(userKey, sandboxId, AccessRequirement.Manage, cancellationToken);

    private async Task<SandboxAccessResult> CheckAsync(UserKey userKey, Guid sandboxId, AccessRequirement requirement, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userKey.Value))
            return SandboxAccessResult.Denied(SandboxAccessFailure.InvalidUser);

        var member = await db.SandboxMembers
            .AsNoTracking()
            .Where(x =>
                x.UserKey == userKey &&
                x.SandboxId == sandboxId)
            .Select(x => new
            {
                x.Role,
                x.SandboxId,
                x.Sandbox.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (member is null)
            return SandboxAccessResult.Denied(SandboxAccessFailure.NotMember);

        if (member.Status == SandboxStatus.Suspended)
            return SandboxAccessResult.Denied(SandboxAccessFailure.Suspended, member.SandboxId, member.Role, member.Status);

        if (requirement is AccessRequirement.Access or AccessRequirement.Read)
        {
            return SandboxAccessResult.Success(member.SandboxId, member.Role, member.Status);
        }

        if (member.Status != SandboxStatus.Active)
            return SandboxAccessResult.Denied(SandboxAccessFailure.Passive, member.SandboxId, member.Role, member.Status);

        if (requirement == AccessRequirement.Manage && member.Role != SandboxMemberRole.Owner)
        {
            return SandboxAccessResult.Denied(SandboxAccessFailure.OwnerRequired, member.SandboxId, member.Role, member.Status);
        }

        return SandboxAccessResult.Success(member.SandboxId, member.Role, member.Status);
    }

    private enum AccessRequirement
    {
        Access,
        Read,
        Write,
        Manage
    }
}
