namespace CodeBeam.UltimateAuth.Core.MultiTenancy;

public sealed class TenantContext
{
    public string? TenantId { get; }
    public bool IsGlobal { get; }

    public TenantContext(string? tenantId, bool isGlobal = false)
    {
        TenantId = tenantId;
        IsGlobal = isGlobal;
    }
}
