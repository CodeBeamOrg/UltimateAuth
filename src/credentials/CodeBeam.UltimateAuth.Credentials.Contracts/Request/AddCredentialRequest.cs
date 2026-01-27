namespace CodeBeam.UltimateAuth.Credentials.Contracts
{
    public sealed record AddCredentialRequest()
    {
        public CredentialType Type { get; set; }
        public required string Secret { get; set; }
        public string? Source { get; set; }
    }
}
