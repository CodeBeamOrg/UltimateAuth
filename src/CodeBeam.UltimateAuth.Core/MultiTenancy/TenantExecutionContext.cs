namespace CodeBeam.UltimateAuth.Core.MultiTenancy;

public sealed class TenantExecutionContext
{
    public TenantKey Tenant { get; }
    public bool IsGlobal { get; }

    public TenantExecutionContext(TenantKey tenant, bool isGlobal = false)
    {
        Tenant = tenant;
        IsGlobal = isGlobal;
    }
}
