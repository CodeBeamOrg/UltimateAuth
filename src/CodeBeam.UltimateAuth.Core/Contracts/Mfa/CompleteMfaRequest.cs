namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record CompleteMfaRequest
{
    public required string ChallengeId { get; init; }
    public required string Code { get; init; }
}
