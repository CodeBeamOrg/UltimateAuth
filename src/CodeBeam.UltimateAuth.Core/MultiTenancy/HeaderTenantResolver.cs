namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    /// <summary>
    /// Resolves tenant id from a request header.
    /// Example: X-Tenant: foo → foo
    /// </summary>
    public sealed class HeaderTenantResolver : ITenantResolver
    {
        private readonly string _headerName;

        public HeaderTenantResolver(string headerName)
        {
            _headerName = headerName;
        }

        public Task<string?> ResolveTenantIdAsync(TenantResolutionContext context)
        {
            if (context.Headers != null &&
                context.Headers.TryGetValue(_headerName, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return Task.FromResult<string?>(value);
            }

            return Task.FromResult<string?>(null);
        }
    }
}
