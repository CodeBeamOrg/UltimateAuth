namespace CodeBeam.UltimateAuth.Server.Options;

/// <summary>
/// Describes how UAuthHub is deployed relative to the application.
/// This affects cookie SameSite, Secure requirements and auth flow defaults.
/// </summary>
public enum UAuthHubDeploymentMode
{
    /// <summary>
    /// UAuthHub is embedded in the same application and same origin.
    /// Example: Blazor Server app hosting auth endpoints internally.
    /// </summary>
    Embedded,

    /// <summary>
    /// UAuthHub is hosted separately but within the same site boundary.
    /// Example: auth.company.com and app.company.com behind same-site policy.
    /// </summary>
    Integrated,

    /// <summary>
    /// UAuthHub is hosted on a different site / domain.
    /// Example: auth.vendor.com used by app.company.com.
    /// </summary>
    External
}
