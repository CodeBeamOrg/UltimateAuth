namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    namespace CodeBeam.UltimateAuth.Core.MultiTenancy
    {
        /// <summary>
        /// Resolves the tenant id based on the request host name.
        /// Example: foo.example.com → returns "foo".
        /// Useful in subdomain-based multi-tenant architectures.
        /// </summary>
        public sealed class HostTenantResolver : ITenantResolver
        {
            /// <summary>
            /// Attempts to resolve the tenant id from the host portion of the incoming request.
            /// Returns null if the host is missing, invalid, or does not contain a subdomain.
            /// </summary>
            public Task<string?> ResolveTenantIdAsync(TenantResolutionContext context)
            {
                var host = context.Host;

                if (string.IsNullOrWhiteSpace(host))
                    return Task.FromResult<string?>(null);

                var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);

                // Expecting at least: {tenant}.{domain}.{tld}
                if (parts.Length < 3)
                    return Task.FromResult<string?>(null);

                return Task.FromResult<string?>(parts[0]);
            }
        }

    }
}
