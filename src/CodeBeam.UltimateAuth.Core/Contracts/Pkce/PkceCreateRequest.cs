namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record PkceCreateRequest
    {
        public string ClientId { get; init; } = default!;
    }
}
