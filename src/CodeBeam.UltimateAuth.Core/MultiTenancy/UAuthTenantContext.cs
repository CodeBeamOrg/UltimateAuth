namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    /// <summary>
    /// Represents the resolved tenant result for the current request.
    /// </summary>
    public sealed class UAuthTenantContext
    {
        public string? TenantId { get; }
        public bool IsResolved { get; }

        private UAuthTenantContext(string? tenantId, bool resolved)
        {
            TenantId = tenantId;
            IsResolved = resolved;
        }

        public static UAuthTenantContext NotResolved()
            => new(null, false);

        public static UAuthTenantContext Resolved(string tenantId)
            => new(tenantId, true);
    }
}
