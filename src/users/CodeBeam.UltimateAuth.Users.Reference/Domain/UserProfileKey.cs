using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.Reference;

public readonly record struct UserProfileKey(
    TenantKey Tenant,
    UserKey UserKey);
