namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    /// <summary>
    /// Resolves the tenant id from a specific HTTP header.
    /// Example: X-Tenant: foo → returns "foo".
    /// Useful when multi-tenancy is controlled by API gateways or reverse proxies.
    /// </summary>
    public sealed class HeaderTenantResolver : ITenantResolver
    {
        private readonly string _headerName;

        /// <summary>
        /// Creates a resolver that reads the tenant id from the given header name.
        /// </summary>
        /// <param name="headerName">The name of the HTTP header to inspect.</param>
        public HeaderTenantResolver(string headerName)
        {
            _headerName = headerName;
        }

        /// <summary>
        /// Attempts to resolve the tenant id by reading the configured header from the request context.
        /// Returns null if the header is missing or empty.
        /// </summary>
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
