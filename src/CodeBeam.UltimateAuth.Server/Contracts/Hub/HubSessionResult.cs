namespace CodeBeam.UltimateAuth.Server.Contracts;

public sealed record HubSessionResult
{
    public string HubSessionId { get; init; } = default!;
}
