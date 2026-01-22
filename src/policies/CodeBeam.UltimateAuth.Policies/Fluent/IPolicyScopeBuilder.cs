namespace CodeBeam.UltimateAuth.Policies
{
    public interface IPolicyScopeBuilder
    {
        IPolicyScopeBuilder RequireAuthenticated();
        IPolicyScopeBuilder RequireSelf();
        IPolicyScopeBuilder RequireAdmin();
        IPolicyScopeBuilder RequireSelfOrAdmin();
        IPolicyScopeBuilder DenyCrossTenant();
    }
}
