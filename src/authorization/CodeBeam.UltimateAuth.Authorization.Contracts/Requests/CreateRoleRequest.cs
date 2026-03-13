namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed class CreateRoleRequest
{
    public string Name { get; set; } = default!;
    public IEnumerable<Permission>? Permissions { get; set; }
}
