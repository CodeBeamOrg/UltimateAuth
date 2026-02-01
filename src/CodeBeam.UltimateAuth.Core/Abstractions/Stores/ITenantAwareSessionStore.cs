namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface ITenantAwareSessionStore
{
    void BindTenant(string? tenantId);
}
