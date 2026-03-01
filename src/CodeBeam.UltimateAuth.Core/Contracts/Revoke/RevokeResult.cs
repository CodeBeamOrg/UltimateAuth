namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record RevokeResult
    {
        public bool CurrentSessionRevoked { get; init; }
        public bool RootRevoked { get; init; }
    }
}
