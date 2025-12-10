namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    /// <summary>
    /// Resolves tenant id from the host.
    /// Example: foo.example.com → foo
    /// </summary>
    public sealed class HostTenantResolver : ITenantResolver
    {
        public Task<string?> ResolveTenantIdAsync(TenantResolutionContext context)
        {
            var host = context.Host;

            if (string.IsNullOrWhiteSpace(host))
                return Task.FromResult<string?>(null);

            var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
                return Task.FromResult<string?>(null);

            return Task.FromResult<string?>(parts[0]);
        }
    }
}
