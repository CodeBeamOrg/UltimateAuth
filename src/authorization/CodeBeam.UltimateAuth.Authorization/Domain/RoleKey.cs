using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.Domain;

public readonly record struct RoleKey(
    TenantKey Tenant,
    RoleId RoleId);