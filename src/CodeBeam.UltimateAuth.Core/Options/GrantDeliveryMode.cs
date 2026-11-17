namespace CodeBeam.UltimateAuth.Core.Options;

/// <summary>
/// Defines how an issued authentication grant is delivered to the client in an HTTP response.
/// </summary>
public enum GrantDeliveryMode
{
    /// <summary>
    /// The grant is not delivered.
    /// </summary>
    None = 0,

    /// <summary>
    /// The grant is delivered through an HTTP cookie.
    /// </summary>
    Cookie = 10,

    /// <summary>
    /// The grant is delivered through an HTTP response header.
    /// </summary>
    Header = 20,

    /// <summary>
    /// The grant is delivered in the HTTP response body.
    /// </summary>
    Body = 30
}
