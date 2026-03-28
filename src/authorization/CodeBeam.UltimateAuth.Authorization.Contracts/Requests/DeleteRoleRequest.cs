using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record DeleteRoleRequest
{
    public required RoleId Id { get; init; }
    public DeleteMode Mode { get; init; }
}
