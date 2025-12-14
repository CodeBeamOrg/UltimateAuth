namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record CompleteMfaRequest
    {
        public string ChallengeId { get; init; } = default!;
        public string Code { get; init; } = default!;
    }
}
