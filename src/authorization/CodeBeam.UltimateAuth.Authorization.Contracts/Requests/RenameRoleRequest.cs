namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record RenameRoleRequest
{
    public required RoleId Id { get; init; }
    public required string Name { get; init; }
}
