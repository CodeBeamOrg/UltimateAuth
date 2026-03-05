using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.Abstractions;

public sealed class Role : IVersionedEntity
{
    public required string Name { get; init; }
    public IReadOnlyCollection<Permission> Permissions { get; init; } = Array.Empty<Permission>();

    public long Version { get; set; }
}
