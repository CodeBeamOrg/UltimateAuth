namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record CompleteMfaSetupRequest
{
    public MfaMethod Method { get; init; }
    public required string VerificationCode { get; init; }
}
