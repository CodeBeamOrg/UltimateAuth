namespace CodeBeam.UltimateAuth.Client.Contracts
{
    public sealed record AuthValidationResult
    {
        public bool IsValid { get; init; }
        public string? State { get; init; }
    }

}
