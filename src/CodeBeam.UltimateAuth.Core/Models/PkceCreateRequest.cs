namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record PkceCreateRequest
    {
        public string ClientId { get; init; } = default!;
    }
}
