namespace CodeBeam.UltimateAuth.Server.Contracts
{
    public sealed record ValidateResponse
    {
        public bool Valid { get; init; }

        public string? State { get; init; }
    }
}
