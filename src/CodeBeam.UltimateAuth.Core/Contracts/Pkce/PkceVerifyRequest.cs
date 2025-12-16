namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record PkceVerifyRequest
    {
        public string Challenge { get; init; } = default!;
        public string Verifier { get; init; } = default!;
    }
}
