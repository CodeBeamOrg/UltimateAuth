namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    /// <summary>
    /// Executes multiple tenant resolvers in order; the first resolver returning a non-null tenant id wins.
    /// </summary>
    public sealed class CompositeTenantResolver : ITenantResolver
    {
        private readonly IReadOnlyList<ITenantResolver> _resolvers;

        /// <summary>
        /// Creates a composite resolver that will evaluate the provided resolvers sequentially.
        /// </summary>
        /// <param name="resolvers">Ordered list of resolvers to execute.</param>
        public CompositeTenantResolver(IEnumerable<ITenantResolver> resolvers)
        {
            _resolvers = resolvers.ToList();
        }

        /// <summary>
        /// Executes each resolver in sequence and returns the first non-null tenant id.
        /// Returns null if no resolver can determine a tenant id.
        /// </summary>
        /// <param name="context">Resolution context containing user id, session, request metadata, etc.</param>
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
