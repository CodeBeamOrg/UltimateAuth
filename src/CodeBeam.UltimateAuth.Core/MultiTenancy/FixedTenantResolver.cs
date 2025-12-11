namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    /// <summary>
    /// Returns a constant tenant id for all resolution requests; useful for single-tenant or statically configured systems.
    /// </summary>
    public sealed class FixedTenantResolver : ITenantIdResolver
    {
        private readonly string _tenantId;

        /// <summary>
        /// Creates a resolver that always returns the specified tenant id.
        /// </summary>
        /// <param name="tenantId">The tenant id that will be returned for all requests.</param>
        public FixedTenantResolver(string tenantId)
        {
            _tenantId = tenantId;
        }

        /// <summary>
        /// Returns the fixed tenant id regardless of context.
        /// </summary>
        public Task<string?> ResolveTenantIdAsync(TenantResolutionContext context)
        {
            return Task.FromResult<string?>(_tenantId);
        }
    }
}
