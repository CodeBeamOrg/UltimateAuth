namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed class SetPermissionsRequest
{
    public IEnumerable<Permission> Permissions { get; set; } = [];
}
