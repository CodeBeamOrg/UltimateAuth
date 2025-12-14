namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record PkceChallengeResult
    {
        public string Challenge { get; init; } = default!;
        public string Method { get; init; } = "S256";
    }
}
