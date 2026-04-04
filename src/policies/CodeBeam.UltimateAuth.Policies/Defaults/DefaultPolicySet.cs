using CodeBeam.UltimateAuth.Authorization.Policies;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Policies.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Policies.Defaults;

internal static class DefaultPolicySet
{
    public static void RegisterServer(AccessPolicyRegistry registry)
    {
        // Invariant
        registry.Add("", _ => new RequireAuthenticatedPolicy());
        registry.Add("", _ => new DenyCrossTenantPolicy());
        registry.Add("", sp => new RequireActiveUserPolicy(sp.GetRequiredService<IUserRuntimeStateProvider>()));

        // Intent-based
        registry.Add("", _ => new RequireSelfPolicy());
        registry.Add("", _ => new DenyAdminSelfModificationPolicy());
        registry.Add("", _ => new RequireSystemPolicy());

        // Permission
        registry.Add("", _ => new MustHavePermissionPolicy());
    }

    public static void RegisterResource(AccessPolicyRegistry registry)
    {
        // Invariant
        registry.Add("", _ => new RequireAuthenticatedPolicy());
        registry.Add("", _ => new DenyCrossTenantPolicy());

        // Intent-based
        registry.Add("", _ => new RequireSelfPolicy());
        registry.Add("", _ => new DenyAdminSelfModificationPolicy());
        registry.Add("", _ => new RequireSystemPolicy());

        // Permission
        registry.Add("", _ => new MustHavePermissionPolicy());
    }
}
