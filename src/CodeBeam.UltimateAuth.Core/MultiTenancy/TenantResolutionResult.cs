namespace CodeBeam.UltimateAuth.Core.MultiTenancy;

public sealed record TenantResolutionResult
{
    public bool IsResolved { get; }
    public TenantKey Tenant { get; }

    private TenantResolutionResult(bool isResolved, TenantKey tenant)
    {
        IsResolved = isResolved;
        Tenant = tenant;
    }

    /// <summary>
    /// Indicates that no tenant could be resolved from the request.
    /// </summary>
    public static TenantResolutionResult NotResolved() => new(isResolved: false, tenant: TenantKey.Unresolved);

    /// <summary>
    /// Indicates that a tenant has been successfully resolved.
    /// </summary>
    public static TenantResolutionResult Resolved(TenantKey tenant) => new(isResolved: true, tenant);
}
