namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record PkceVerificationResult
    {
        public bool IsValid { get; init; }
    }
}
