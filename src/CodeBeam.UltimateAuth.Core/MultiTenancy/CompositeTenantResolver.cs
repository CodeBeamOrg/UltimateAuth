namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    /// <summary>
    /// Executes multiple tenant resolvers in order.
    /// The first resolver that returns a non-null tenant id wins.
    /// </summary>
    public sealed class CompositeTenantResolver : ITenantResolver
    {
        private readonly IReadOnlyList<ITenantResolver> _resolvers;

        public CompositeTenantResolver(IEnumerable<ITenantResolver> resolvers)
        {
            _resolvers = resolvers.ToList();
        }

        public async Task<string?> ResolveTenantIdAsync(TenantResolutionContext context)
        {
            foreach (var resolver in _resolvers)
            {
                var tid = await resolver.ResolveTenantIdAsync(context);
                if (!string.IsNullOrWhiteSpace(tid))
                    return tid;
            }

            return null;
        }
    }
}
