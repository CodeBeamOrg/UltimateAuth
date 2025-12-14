namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record MfaChallengeResult
    {
        public string ChallengeId { get; init; } = default!;
        public string Method { get; init; } = default!; // totp, sms, email etc.
    }
}
