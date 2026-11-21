using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

/// <summary>
/// Configures how an individual authentication grant is delivered to the client.
/// </summary>
/// <remarks>
/// These settings override the global grant delivery policy.
///
/// Grant identity, token format, and issuance behavior are determined by the effective authentication mode 
/// and cannot be changed through delivery options.
/// </remarks>
public sealed class GrantDeliveryOptions
{
    /// <summary>
    /// Gets or sets the delivery mode for this grant.
    /// </summary>
    /// <remarks>
    /// Null inherits the global delivery preference or the authentication mode and application profile defaults when no global preference is configured.
    /// </remarks>
    public GrantDeliveryMode? Mode { get; set; }

    /// <summary>
    /// Gets or sets the HTTP response header name when delivery mode is Header.
    /// </summary>
    public string? HeaderName { get; set; }

    /// <summary>
    /// Gets or sets the header value format when delivery mode is Header.
    /// </summary>
    public HeaderTokenFormat? HeaderFormat { get; set; }

    internal GrantDeliveryOptions Clone() => new()
    {
        Mode = Mode,
        HeaderName = HeaderName,
        HeaderFormat = HeaderFormat
    };
}
