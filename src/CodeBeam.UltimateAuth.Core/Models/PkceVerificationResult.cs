namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record PkceVerificationResult
    {
        public bool IsValid { get; init; }
    }
}
