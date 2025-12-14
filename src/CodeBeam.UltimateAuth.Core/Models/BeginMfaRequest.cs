namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record BeginMfaRequest
    {
        public string MfaToken { get; init; } = default!;
    }
}
