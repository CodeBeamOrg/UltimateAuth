namespace CodeBeam.UltimateAuth.Client.Contracts
{
    internal sealed class PkceClientState
    {
        public string Verifier { get; init; } = default!;
        public string AuthorizationCode { get; init; } = default!;
    }
}
