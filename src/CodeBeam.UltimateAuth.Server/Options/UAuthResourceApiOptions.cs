using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

public class UAuthResourceApiOptions
{
    public UAuthMode AuthMode { get; set; }
    public string UAuthHubBaseUrl { get; set; } = default!;
    public HashSet<string> AllowedClientOrigins { get; set; } = new();
    public string CorsPolicyName { get; set; } = "UAuthResource";
    public UAuthMultiTenantOptions MultiTenant { get; set; } = new();

    internal UAuthResourceApiOptions Clone() => new()
    {
        AuthMode = AuthMode,
        UAuthHubBaseUrl = UAuthHubBaseUrl,
        AllowedClientOrigins = new HashSet<string>(AllowedClientOrigins),
        CorsPolicyName = CorsPolicyName,
        MultiTenant = MultiTenant.Clone()
    };
}
