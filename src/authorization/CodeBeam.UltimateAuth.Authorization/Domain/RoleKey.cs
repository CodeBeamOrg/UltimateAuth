using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization;

public readonly record struct RoleKey(
    TenantKey Tenant,
    RoleId RoleId);