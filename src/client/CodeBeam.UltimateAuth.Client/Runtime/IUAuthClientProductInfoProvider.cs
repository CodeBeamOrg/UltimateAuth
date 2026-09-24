namespace CodeBeam.UltimateAuth.Client.Runtime;

/// <summary>
/// Provides information about the product using the UltimateAuth client.
/// </summary>
public interface IUAuthClientProductInfoProvider
{
    /// <summary>
    /// Gets the product information for the UltimateAuth client.
    /// </summary>
    UAuthClientProductInfo Get();
}
