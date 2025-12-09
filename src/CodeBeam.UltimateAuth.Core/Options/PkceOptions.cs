namespace CodeBeam.UltimateAuth.Core.Options
{
    public sealed class PkceOptions
    {
        /// <summary>
        /// Authorization code lifetime for PKCE flows.
        /// </summary>
        public int AuthorizationCodeLifetimeSeconds { get; set; } = 120;
    }
}
