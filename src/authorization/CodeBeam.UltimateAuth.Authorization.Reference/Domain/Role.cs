using CodeBeam.UltimateAuth.Authorization.Domain;

public sealed class Role
{
    public required string Name { get; init; }
    public IReadOnlyCollection<Permission> Permissions { get; init; } = Array.Empty<Permission>();
}
