namespace CodeBeam.UltimateAuth.Core.MultiTenancy;

public sealed class TenantContext
{
    public TenantKey Tenant { get; }
    public bool IsGlobal { get; }

    public TenantContext(TenantKey tenant, bool isGlobal = false)
    {
        Tenant = tenant;
        IsGlobal = isGlobal;
    }
}
