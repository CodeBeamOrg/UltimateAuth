using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record OrphanUserItem(UserKey UserKey, string DisplayName, string? Email, string Role, DateTimeOffset CreatedAt, Guid? OwnedSandboxId);

public sealed record EmptySandboxItem(Guid SandboxId, string Name, UserKey OwnerUserKey, SandboxStatus Status, DateTimeOffset CreatedAt);

public sealed record RepairSnapshot(IReadOnlyList<OrphanUserItem> OrphanUsers, IReadOnlyList<EmptySandboxItem> EmptySandboxes, bool UserScanComplete);

public sealed record RepairOperationResult(bool Success, string Message)
{
    public static RepairOperationResult Ok(string message)
        => new(true, message);

    public static RepairOperationResult Fail(string message)
        => new(false, message);
}
