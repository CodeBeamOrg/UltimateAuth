namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record CompleteMfaSetupRequest
{
    public MfaMethod Method { get; init; }
    public string VerificationCode { get; init; } = default!;
}
