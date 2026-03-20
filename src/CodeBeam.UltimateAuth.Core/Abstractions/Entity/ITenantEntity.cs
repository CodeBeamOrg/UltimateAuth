using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface ITenantEntity
{
    TenantKey Tenant { get; }
}
