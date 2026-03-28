namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record BeginMfaRequest
{
    public required string MfaToken { get; init; }
}
