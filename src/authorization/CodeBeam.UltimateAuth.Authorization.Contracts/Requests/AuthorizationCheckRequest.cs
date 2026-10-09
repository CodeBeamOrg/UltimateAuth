namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record AuthorizationCheckRequest
{
    public required string Action { get; init; }
    public required string Resource { get; init; }
    public string? ResourceId { get; init; }
}
