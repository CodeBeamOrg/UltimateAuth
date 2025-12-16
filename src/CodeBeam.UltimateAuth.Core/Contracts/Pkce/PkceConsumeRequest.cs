namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record PkceConsumeRequest
    {
        public string Challenge { get; init; } = default!;
    }
}
