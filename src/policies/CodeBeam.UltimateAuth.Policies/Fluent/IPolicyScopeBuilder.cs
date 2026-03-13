namespace CodeBeam.UltimateAuth.Policies;

public interface IPolicyScopeBuilder
{
    IPolicyScopeBuilder RequireAuthenticated();
    IPolicyScopeBuilder RequireSelf();
    IPolicyScopeBuilder RequirePermission();
    IPolicyScopeBuilder DenyCrossTenant();
}
