using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.Reference;

public readonly record struct UserLifecycleKey(
    TenantKey Tenant,
    UserKey UserKey);
