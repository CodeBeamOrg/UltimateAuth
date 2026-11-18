using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

public class UAuthResourceApiOptions
{
    public UAuthMode AuthMode { get; set; }
    public GrantDeliveryMode SessionDeliveryMode { get; set; } = GrantDeliveryMode.Cookie;
    public GrantDeliveryMode AccessTokenDeliveryMode { get; set; } = GrantDeliveryMode.Cookie;
    public string SessionCookieName { get; set; } = "uas";
    public string AccessTokenCookieName { get; set; } = "uat";
    public string AccessTokenHeaderName { get; set; } = "Authorization";
    public HeaderTokenFormat AccessTokenHeaderFormat { get; set; } = HeaderTokenFormat.Bearer;
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
        MultiTenant = MultiTenant.Clone(),
        SessionDeliveryMode = SessionDeliveryMode,
        AccessTokenDeliveryMode = AccessTokenDeliveryMode,
        SessionCookieName = SessionCookieName,
        AccessTokenCookieName = AccessTokenCookieName,
        AccessTokenHeaderName = AccessTokenHeaderName,
        AccessTokenHeaderFormat = AccessTokenHeaderFormat
    };
}
