namespace CodeBeam.UltimateAuth.Core.Options
{
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
        /// If tenant cannot be resolved, this value is used.
        /// If null and RequireTenant = true, request fails.
        /// </summary>
        public string? DefaultTenantId { get; set; }

        /// <summary>
        /// If true, a resolved tenant id must always exist.
        /// If resolver cannot determine tenant, request will fail.
        /// </summary>
        public bool RequireTenant { get; set; } = false;

        /// <summary>
        /// If true, a tenant id returned by resolver does NOT need to be known beforehand.
        /// If false, unknown tenants must be explicitly registered.
        /// (Useful for multi-tenant SaaS with dynamic tenant provisioning)
        /// </summary>
        public bool AllowUnknownTenants { get; set; } = true;

        /// <summary>
        /// Tenant ids that cannot be used by clients.
        /// Protects system-level tenant identifiers.
        /// </summary>
        public HashSet<string> ReservedTenantIds { get; set; } = new()
        {
            "system",
            "root",
            "admin",
            "public"
        };

        /// <summary>
        /// If true, tenant identifiers are normalized to lowercase.
        /// Recommended for host-based tenancy.
        /// </summary>
        public bool NormalizeToLowercase { get; set; } = true;

        /// <summary>
        /// Optional validation for tenant id format.
        /// Default: alphanumeric + hyphens allowed.
        /// </summary>
        public string TenantIdRegex { get; set; } = "^[a-zA-Z0-9\\-]+$";

        /// <summary>
        /// Enables tenant resolution from the URL path and
        /// exposes auth endpoints under /{tenant}/{routePrefix}/...
        /// </summary>
        public bool EnableRoute { get; set; } = true;
        public bool EnableHeader { get; set; } = false;
        public bool EnableDomain { get; set; } = false;

        // Header config
        public string HeaderName { get; set; } = "X-Tenant";
    }
}
