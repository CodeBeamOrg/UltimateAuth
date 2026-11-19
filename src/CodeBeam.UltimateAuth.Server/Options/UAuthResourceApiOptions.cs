using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthResourceApiOptions
{
    public UAuthResourceAuthOptions Auth { get; set; } = new();

    public UAuthResourceHubOptions Hub { get; set; } = new();

    public UAuthResourceCorsOptions Cors { get; set; } = new();

    public UAuthMultiTenantOptions MultiTenant { get; set; } = new();

    internal UAuthResourceApiOptions Clone() => new()
    {
        Auth = Auth.Clone(),
        Hub = Hub.Clone(),
        Cors = Cors.Clone(),
        MultiTenant = MultiTenant.Clone()
    };
}

/// <summary>
/// Authentication, authorization and security-related
/// behavior for the Resource API.
/// </summary>
public sealed class UAuthResourceAuthOptions
{
    public UAuthMode Mode { get; set; }

    public UAuthResourceSessionOptions Session { get; set; } = new();

    public UAuthResourceAccessTokenOptions AccessToken { get; set; } = new();

    public UAuthResourceDeviceOptions Device { get; set; } = new();

    public UAuthJwtValidationOptions? Jwt { get; set; }

    internal UAuthResourceAuthOptions Clone() => new()
    {
        Mode = Mode,
        Session = Session.Clone(),
        AccessToken = AccessToken.Clone(),
        Device = Device.Clone(),
        Jwt = Jwt is null
            ? null
            : new UAuthJwtValidationOptions
            {
                Issuer = Jwt.Issuer,
                Audience = Jwt.Audience,
                ValidAlgorithms = Jwt.ValidAlgorithms.ToArray(),
                ClockSkew = Jwt.ClockSkew,
                MaximumTokenSizeInBytes = Jwt.MaximumTokenSizeInBytes
            }
    };
}

/// <summary>
/// Incoming opaque session credential configuration.
/// </summary>
public sealed class UAuthResourceSessionOptions
{
    public GrantDeliveryMode DeliveryMode { get; set; } = GrantDeliveryMode.Header;

    public string CookieName { get; set; } = "uas";

    internal UAuthResourceSessionOptions Clone() => new()
    {
        DeliveryMode = DeliveryMode,
        CookieName = CookieName
    };
}

/// <summary>
/// Incoming access token credential configuration.
/// </summary>
public sealed class UAuthResourceAccessTokenOptions
{
    public GrantDeliveryMode DeliveryMode { get; set; } = GrantDeliveryMode.Header;

    public string CookieName { get; set; } = "uat";

    public string HeaderName { get; set; } = "Authorization";

    public HeaderTokenFormat HeaderFormat { get; set; } = HeaderTokenFormat.Bearer;

    internal UAuthResourceAccessTokenOptions Clone() => new()
    {
        DeliveryMode = DeliveryMode,
        CookieName = CookieName,
        HeaderName = HeaderName,
        HeaderFormat = HeaderFormat
    };
}

/// <summary>
/// Device security policies applied during credential validation.
/// </summary>
public sealed class UAuthResourceDeviceOptions
{
    public DeviceMismatchBehavior MismatchBehavior { get; set; } = DeviceMismatchBehavior.Reject;

    internal UAuthResourceDeviceOptions Clone() => new()
    {
        MismatchBehavior = MismatchBehavior
    };
}

/// <summary>
/// Configuration for communication with the UltimateAuth Hub.
/// </summary>
public sealed class UAuthResourceHubOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    internal UAuthResourceHubOptions Clone() => new()
    {
        BaseUrl = BaseUrl
    };
}

/// <summary>
/// Resource API CORS configuration.
/// </summary>
public sealed class UAuthResourceCorsOptions
{
    public HashSet<string> AllowedOrigins { get; set; } = new();

    public string PolicyName { get; set; } = "UAuthResource";

    internal UAuthResourceCorsOptions Clone() => new()
    {
        AllowedOrigins = new HashSet<string>(AllowedOrigins, AllowedOrigins.Comparer),
        PolicyName = PolicyName
    };
}
