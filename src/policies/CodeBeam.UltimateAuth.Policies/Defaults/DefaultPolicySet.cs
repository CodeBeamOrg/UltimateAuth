using CodeBeam.UltimateAuth.Policies.Registry;

namespace CodeBeam.UltimateAuth.Policies.Defaults;

internal static class DefaultPolicySet
{
    public static void Register(AccessPolicyRegistry registry)
    {
        // Globals
        registry.Add("", _ => new RequireAuthenticatedPolicy());
        registry.Add("", _ => new DenyCrossTenantPolicy());

        // Self operations
        registry.Add("users.profile.", _ => new RequireSelfPolicy());
        registry.Add("credentials.self.", _ => new RequireSelfPolicy());

        // Admin-only
        registry.Add("admin.", _ => new RequireAdminPolicy());

        // Self OR admin
        registry.Add("users.", _ => new RequireSelfOrAdminPolicy());
    }
}
