namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record PermissionDto
{
    public required string Value { get; init; }
}
