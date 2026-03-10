namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record RoleInfo
{
    public RoleId Id { get; init; }
    public required string Name { get; init; }
}
