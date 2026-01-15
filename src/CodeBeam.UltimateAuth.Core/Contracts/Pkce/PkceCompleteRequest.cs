namespace CodeBeam.UltimateAuth.Core.Contracts
{
    internal sealed class PkceCompleteRequest
    {
        public string AuthorizationCode { get; init; } = default!;
        public string CodeVerifier { get; init; } = default!;
        public string Identifier { get; init; } = default!;
        public string Secret { get; init; } = default!;
        public string ReturnUrl { get; init; } = default!;
    }
}
