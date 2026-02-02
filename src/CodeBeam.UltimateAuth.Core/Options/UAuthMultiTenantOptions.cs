namespace CodeBeam.UltimateAuth.Core.Options;

/// <summary>
/// Multi-tenancy configuration for UltimateAuth.
/// Controls whether tenants are required, how they are resolved,
/// and how tenant identifiers are normalized.
/// </summary>
public sealed class UAuthMultiTenantOptions
{
    /// <summary>
    /// Enables multi-tenant mode.
    /// When disabled, all requests operate under a single implicit tenant.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// If true, tenant resolution MUST succeed for external requests.
    /// If false, unresolved tenants fall back to single-tenant behavior.
    /// </summary>
    public bool RequireTenant { get; set; } = false;

    /// <summary>
    /// If true, a tenant id returned by resolver does NOT need to be known beforehand.
    /// If false, unknown tenants must be explicitly registered.
    /// (Useful for multi-tenant SaaS with dynamic tenant provisioning)
    /// </summary>
    public bool AllowUnknownTenants { get; set; } = true;

    /// <summary>
    /// If true, tenant identifiers are normalized to lowercase.
    /// Recommended for host-based tenancy.
    /// </summary>
    public bool NormalizeToLowercase { get; set; } = true;


    /// <summary>
    /// Enables tenant resolution from the URL path and
    /// exposes auth endpoints under /{tenant}/{routePrefix}/...
    /// </summary>
    public bool EnableRoute { get; set; } = true;
    public bool EnableHeader { get; set; } = false;
    public bool EnableDomain { get; set; } = false;

    // Header config
    public string HeaderName { get; set; } = "X-Tenant";

    internal UAuthMultiTenantOptions Clone() => new()
    {
        Enabled = Enabled,
        RequireTenant = RequireTenant,
        AllowUnknownTenants = AllowUnknownTenants,
        NormalizeToLowercase = NormalizeToLowercase,
        EnableRoute = EnableRoute,
        EnableHeader = EnableHeader,
        EnableDomain = EnableDomain,
        HeaderName = HeaderName
    };

}
