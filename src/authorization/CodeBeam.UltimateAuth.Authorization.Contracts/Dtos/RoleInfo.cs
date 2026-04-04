namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record RoleInfo
{
    public RoleId Id { get; init; }
    public required string Name { get; init; }

    public IReadOnlyCollection<Permission> Permissions { get; init; } = Array.Empty<Permission>();

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
