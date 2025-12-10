namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    /// <summary>
    /// Represents the normalized request information used during tenant resolution.
    /// Resolvers inspect these fields to derive the correct tenant id.
    /// </summary>
    public sealed class TenantResolutionContext
    {
        /// <summary>
        /// The request host value (e.g., "foo.example.com").
        /// Used by HostTenantResolver.
        /// </summary>
        public string? Host { get; init; }

        /// <summary>
        /// The request path (e.g., "/t/foo/api/...").
        /// Used by PathTenantResolver.
        /// </summary>
        public string? Path { get; init; }

        /// <summary>
        /// Request headers. Used by HeaderTenantResolver.
        /// </summary>
        public IReadOnlyDictionary<string, string>? Headers { get; init; }

        /// <summary>
        /// Query string parameters. Used by future resolvers or custom logic.
        /// </summary>
        public IReadOnlyDictionary<string, string>? Query { get; init; }

        /// <summary>
        /// The raw framework-specific request context (e.g., HttpContext).
        /// Used only when advanced resolver logic needs full access.
        /// </summary>
        public object? RawContext { get; init; }
    }
}
