using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

public class UAuthResourceApiOptions
{
    public string UAuthHubBaseUrl { get; set; } = default!;
    public HashSet<string> AllowedClientOrigins { get; set; } = new();
    public string CorsPolicyName { get; set; } = "UAuthResource";
    public UAuthMultiTenantOptions MultiTenant { get; set; } = new();

    internal UAuthResourceApiOptions Clone() => new()
    {
        UAuthHubBaseUrl = UAuthHubBaseUrl,
        AllowedClientOrigins = new HashSet<string>(AllowedClientOrigins),
        CorsPolicyName = CorsPolicyName,
        MultiTenant = MultiTenant.Clone()
    };
}
