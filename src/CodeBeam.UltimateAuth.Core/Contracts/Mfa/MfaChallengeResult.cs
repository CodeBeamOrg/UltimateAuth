namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record MfaChallengeResult
{
    public required string ChallengeId { get; init; }
    public required string Method { get; init; } // totp, sms, email etc.
}
