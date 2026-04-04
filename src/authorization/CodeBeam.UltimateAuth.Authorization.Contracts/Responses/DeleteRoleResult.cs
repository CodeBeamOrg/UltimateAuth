using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed class DeleteRoleResult
{
    public RoleId RoleId { get; init; }

    public int RemovedAssignments { get; init; }

    public DeleteMode Mode { get; init; }

    public DateTimeOffset DeletedAt { get; init; }
}
