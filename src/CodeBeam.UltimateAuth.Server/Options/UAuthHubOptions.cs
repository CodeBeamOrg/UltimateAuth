namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class UAuthHubOptions
    {
        public string? ClientBaseAddress { get; set; }

        public HashSet<string> AllowedClientOrigins { get; set; } = new();
    }

}
