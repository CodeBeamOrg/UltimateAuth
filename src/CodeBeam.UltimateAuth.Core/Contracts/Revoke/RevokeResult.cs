namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record RevokeResult
    {
        public bool CurrentChain { get; init; }
        public bool RootRevoked { get; init; }
    }
}
