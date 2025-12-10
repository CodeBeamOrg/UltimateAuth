namespace CodeBeam.UltimateAuth.Core.MultiTenancy
{
    /// <summary>
    /// Defines a strategy for resolving the tenant id for the current request.
    /// </summary>
    public interface ITenantResolver
    {
        Task<string?> ResolveTenantIdAsync(TenantResolutionContext context);
    }
}
