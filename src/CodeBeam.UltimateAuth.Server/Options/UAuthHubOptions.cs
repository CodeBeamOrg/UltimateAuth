using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class UAuthHubOptions
    {
        public string? ClientBaseAddress { get; set; }

        public HashSet<string> AllowedClientOrigins { get; set; } = new();

        internal UAuthHubOptions Clone() => new()
        {
            ClientBaseAddress = ClientBaseAddress,
            AllowedClientOrigins = new HashSet<string>(AllowedClientOrigins)
        };

    }
}
