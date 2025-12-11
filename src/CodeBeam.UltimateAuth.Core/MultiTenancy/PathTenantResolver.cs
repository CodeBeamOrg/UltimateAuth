namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    /// <summary>
    /// Resolves the tenant id from the request path.
    /// Example pattern: /t/{tenantId}/... → returns the extracted tenant id.
    /// </summary>
    public sealed class PathTenantResolver : ITenantIdResolver
    {
        private readonly string _prefix;

        /// <summary>
        /// Creates a resolver that looks for tenant ids under a specific URL prefix.
        /// Default prefix is "t", meaning URLs like /t/foo/api will resolve "foo".
        /// </summary>
        public PathTenantResolver(string prefix = "t")
        {
            _prefix = prefix;
        }

        /// <summary>
        /// Extracts the tenant id from the request path, if present.
        /// Returns null when the prefix is not matched or the path is insufficient.
        /// </summary>
        public Task<string?> ResolveTenantIdAsync(TenantResolutionContext context)
        {
            var path = context.Path;
            if (string.IsNullOrWhiteSpace(path))
                return Task.FromResult<string?>(null);

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Format: /{prefix}/{tenantId}/...
            if (segments.Length >= 2 && segments[0] == _prefix)
                return Task.FromResult<string?>(segments[1]);

            return Task.FromResult<string?>(null);
        }

    }
}
