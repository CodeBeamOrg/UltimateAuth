namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record PkceVerifyRequest
    {
        public string Challenge { get; init; } = default!;
        public string Verifier { get; init; } = default!;
    }
}
