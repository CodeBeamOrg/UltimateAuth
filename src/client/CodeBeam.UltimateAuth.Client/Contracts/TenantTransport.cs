namespace CodeBeam.UltimateAuth.Client.Contracts;

/// <summary>
/// Specifies how tenant context is transported with UltimateAuth client requests.
/// </summary>
public enum TenantTransport
{
    /// <summary>
    /// Does not explicitly include tenant context in the request transport.
    /// </summary>
    None = 0,

    /// <summary>
    /// Includes tenant context in an HTTP request header.
    /// </summary>
    Header = 10,

    /// <summary>
    /// Includes tenant context as part of the request route.
    /// </summary>
    Route = 20
}
