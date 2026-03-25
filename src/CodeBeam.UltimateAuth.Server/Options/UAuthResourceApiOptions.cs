namespace CodeBeam.UltimateAuth.Server.Options;

public class UAuthResourceApiOptions
{
    public string UAuthHubBaseUrl { get; set; } = default!;
    public HashSet<string> AllowedClientOrigins { get; set; } = new();
    public string CorsPolicyName { get; set; } = "UAuthResource";
}
