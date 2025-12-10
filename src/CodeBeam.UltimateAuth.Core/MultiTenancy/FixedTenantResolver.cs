namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    /// <summary>
    /// Always returns a fixed tenant id.
    /// Useful for single-tenant or statically configured environments.
    /// </summary>
    public sealed class FixedTenantResolver : ITenantResolver
    {
        private readonly string _tenantId;

        public FixedTenantResolver(string tenantId)
        {
            _tenantId = tenantId;
        }

        public Task<string?> ResolveTenantIdAsync(TenantResolutionContext context)
        {
            return Task.FromResult<string?>(_tenantId);
        }
    }
}
