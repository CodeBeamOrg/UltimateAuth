namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record SetPermissionsRequest
{
    public required RoleId Id { get; init; }
    public IEnumerable<Permission> Permissions { get; init; } = [];
}
