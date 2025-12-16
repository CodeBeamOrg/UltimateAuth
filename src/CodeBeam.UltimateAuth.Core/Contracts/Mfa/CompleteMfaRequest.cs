namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record CompleteMfaRequest
    {
        public string ChallengeId { get; init; } = default!;
        public string Code { get; init; } = default!;
    }
}
