namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record CreateRoleRequest
{
    public required string Name { get; init; }
    public IEnumerable<Permission>? Permissions { get; init; }
}
