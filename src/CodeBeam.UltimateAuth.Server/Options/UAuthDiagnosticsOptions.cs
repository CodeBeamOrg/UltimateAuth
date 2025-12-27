namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class UAuthDiagnosticsOptions
    {
        /// <summary>
        /// Enables debug / sample-only response headers such as X-UAuth-Refresh.
        /// Should be disabled in production.
        /// </summary>
        public bool EnableRefreshHeaders { get; set; } = false;
    }
}
