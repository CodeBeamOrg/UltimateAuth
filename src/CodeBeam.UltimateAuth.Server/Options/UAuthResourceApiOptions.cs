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

    public UAuthResourceCryptographyOptions Cryptography { get; set; } = new();

    internal UAuthResourceApiOptions Clone() => new()
    {
        Auth = Auth.Clone(),
        Hub = Hub.Clone(),
        Cors = Cors.Clone(),
        MultiTenant = MultiTenant.Clone(),
        Cryptography = Cryptography.Clone()
    };
}

/// <summary>
/// Authentication, authorization and security-related
/// behavior for the Resource API.
/// </summary>
public sealed class UAuthResourceAuthOptions
{
    /// <summary>
    /// Gets or sets an explicit authentication mode for this Resource API.
    /// </summary>
    /// <remarks>
    /// When null, the registered authentication mode resolver determines the mode using the ResourceApi application profile.
    /// The built-in resolver selects Hybrid.
    /// The resolved mode is fixed for this Resource API runtime.
    /// </remarks>
    public UAuthMode? Mode { get; set; }

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
    public GrantDeliveryMode DeliveryMode { get; set; } = GrantDeliveryMode.Cookie;

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

public sealed class UAuthResourceCryptographyOptions
{
    public List<UAuthJwtVerificationKeyOptions> JwtVerificationKeys { get; set; } = [];

    internal UAuthResourceCryptographyOptions Clone() => new()
    {
        JwtVerificationKeys = JwtVerificationKeys
            .Select(x => new UAuthJwtVerificationKeyOptions
            {
                KeyId = x.KeyId,
                Key = x.Key
            })
            .ToList()
    };
}

public sealed class UAuthJwtVerificationKeyOptions
{
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded symmetric verification key.
    /// Development and explicitly configured symmetric JWT validation.
    /// </summary>
    public string Key { get; set; } = string.Empty;
}
