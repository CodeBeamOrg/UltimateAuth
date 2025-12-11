namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    /// <summary>
    /// Defines a strategy for resolving the tenant id for the current request.
    /// Implementations may extract the tenant from headers, hostnames,
    /// authentication tokens, or any other application-defined source.
    /// </summary>
    public interface ITenantIdResolver
    {
        /// <summary>
        /// Attempts to resolve the tenant id given the contextual request data.
        /// Returns null when no tenant can be determined.
        /// </summary>
        Task<string?> ResolveTenantIdAsync(TenantResolutionContext context);
    }
}
