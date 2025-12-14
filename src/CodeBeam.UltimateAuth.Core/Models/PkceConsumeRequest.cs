namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record PkceConsumeRequest
    {
        public string Challenge { get; init; } = default!;
    }
}
