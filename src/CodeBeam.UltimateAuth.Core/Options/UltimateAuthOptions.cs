namespace CodeBeam.UltimateAuth.Core.Options
{
    public sealed class UltimateAuthOptions
    {
        public LoginOptions Login { get; set; } = new();
        public SessionOptions Session { get; set; } = new();
        public TokenOptions Token { get; set; } = new();
        public PkceOptions Pkce { get; set; } = new();
    }
}
