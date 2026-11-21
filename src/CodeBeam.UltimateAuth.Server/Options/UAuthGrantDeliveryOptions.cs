using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

/// <summary>
/// Configures how authentication grants are delivered to clients.
/// </summary>
/// <remarks>
/// Delivery settings do not control grant issuance, token format, validation, or authentication mode.
///
/// The effective authentication mode determines which grants exist and which delivery configurations are supported.
/// </remarks>
public sealed class UAuthGrantDeliveryOptions
{
    /// <summary>
    /// Gets or sets the default delivery mode for grants issued by the authentication mode.
    /// </summary>
    /// <remarks>
    /// Null uses authentication mode and application profile defaults. Individual grant settings take precedence.
    /// </remarks>
    public GrantDeliveryMode? Default { get; set; }

    /// <summary>
    /// Configures delivery of the session grant.
    /// </summary>
    public GrantDeliveryOptions Session { get; set; } = new();

    /// <summary>
    /// Configures delivery of the access token grant.
    /// </summary>
    public GrantDeliveryOptions AccessToken { get; set; } = new();

    /// <summary>
    /// Configures delivery of the refresh token grant.
    /// </summary>
    public GrantDeliveryOptions RefreshToken { get; set; } = new();

    internal UAuthGrantDeliveryOptions Clone() => new()
    {
        Default = Default,
        Session = Session.Clone(),
        AccessToken = AccessToken.Clone(),
        RefreshToken = RefreshToken.Clone()
    };
}
