namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    /// <summary>
    /// Resolves tenant id from request path.
    /// Example: /t/foo/api/... → foo
    /// </summary>
    public sealed class PathTenantResolver : ITenantResolver
    {
        private readonly string _prefix;

        public PathTenantResolver(string prefix = "t")
        {
            _prefix = prefix;
        }

        public Task<string?> ResolveTenantIdAsync(TenantResolutionContext context)
        {
            var path = context.Path;
            if (string.IsNullOrWhiteSpace(path))
                return Task.FromResult<string?>(null);

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Format: /{prefix}/{tenantId}/...
            if (segments.Length >= 2 && segments[0] == _prefix)
            {
                return Task.FromResult<string?>(segments[1]);
            }

            return Task.FromResult<string?>(null);
        }
    }
}
