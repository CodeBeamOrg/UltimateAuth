namespace CodeBeam.UltimateAuth.Server
{
    /// <summary>
    /// Represents which endpoint groups are enabled by default
    /// for a given authentication mode.
    /// </summary>
    public sealed class UAuthEndpointDefaults
    {
        public bool Login { get; init; }
        public bool Pkce { get; init; }
        public bool Token { get; init; }
        public bool Session { get; init; }
        public bool UserInfo { get; init; }
    }
}
