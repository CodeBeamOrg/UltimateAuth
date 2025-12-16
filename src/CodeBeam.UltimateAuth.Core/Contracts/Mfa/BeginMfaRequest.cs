namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record BeginMfaRequest
    {
        public string MfaToken { get; init; } = default!;
    }
}
