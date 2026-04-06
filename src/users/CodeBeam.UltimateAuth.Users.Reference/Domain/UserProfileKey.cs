using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public readonly record struct UserProfileKey(
    TenantKey Tenant,
    UserKey UserKey,
    ProfileKey ProfileKey);
