namespace CodeBeam.UltimateAuth.Client.Contracts
{
    public sealed record BrowserPostResult
    {
        public bool Ok { get; init; }
        public int Status { get; init; }
        public string? RefreshOutcome { get; init; }
    }
}
