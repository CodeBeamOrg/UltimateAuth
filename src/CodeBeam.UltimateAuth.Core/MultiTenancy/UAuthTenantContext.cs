namespace CodeBeam.UltimateAuth.Core.MultiTenancy;

/// <summary>
/// Represents the resolved tenant result for the current request.
/// </summary>
public sealed class UAuthTenantContext
{
    public TenantKey Tenant { get; }

    private UAuthTenantContext(TenantKey tenant, bool allowUnresolved = false)
    {
        if (!allowUnresolved && tenant.IsUnresolved)
            throw new InvalidOperationException("Runtime tenant context cannot be unresolved.");

        Tenant = tenant;
    }

    public bool IsSingleTenant => Tenant.IsSingle;
    public bool IsSystem => Tenant.IsSystem;

    public static UAuthTenantContext SingleTenant() => new(TenantKey.Single);
    public static UAuthTenantContext System() => new(TenantKey.System);
    public static UAuthTenantContext Unresolved() => new(TenantKey.Unresolved, allowUnresolved: true);
    public static UAuthTenantContext Resolved(TenantKey tenant) => new(tenant);
}
