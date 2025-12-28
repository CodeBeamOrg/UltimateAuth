using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Runtime
{
    public sealed class UAuthServerProductInfo
    {
        public string ProductName { get; init; } = "UltimateAuthServer";
        public UAuthProductInfo Core { get; init; } = default!;

        public UAuthMode? AuthMode { get; init; }
        public UAuthHubDeploymentMode HubDeploymentMode { get; init; }

        public bool PkceEnabled { get; init; }
        public bool RefreshEnabled { get; init; }
        public bool MultiTenancyEnabled { get; init; }
    }
}
