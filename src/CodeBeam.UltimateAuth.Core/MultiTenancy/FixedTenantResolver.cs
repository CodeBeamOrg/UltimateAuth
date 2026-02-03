namespace CodeBeam.UltimateAuth.Core.MultiTenancy;

public sealed class FixedTenantResolver : ITenantIdResolver
{
    private readonly string _tenantId;

    public FixedTenantResolver(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant id cannot be empty.", nameof(tenantId));

        _tenantId = tenantId;
    }

    public Task<string?> ResolveTenantIdAsync(TenantResolutionContext context) => Task.FromResult<string?>(_tenantId);
}
