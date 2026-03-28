namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record SetRolePermissionsRequest
{
    public required RoleId RoleId { get; init; }
    public IEnumerable<Permission> Permissions { get; init; } = [];
}
