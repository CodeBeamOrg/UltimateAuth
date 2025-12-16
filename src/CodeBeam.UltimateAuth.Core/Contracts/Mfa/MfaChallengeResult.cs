namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record MfaChallengeResult
    {
        public string ChallengeId { get; init; } = default!;
        public string Method { get; init; } = default!; // totp, sms, email etc.
    }
}
