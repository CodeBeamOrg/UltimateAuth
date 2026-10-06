using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies;

public interface IPolicyScopeBuilder
{
    IPolicyScopeBuilder RequireAuthenticated();
    IPolicyScopeBuilder RequireSelf();
    IPolicyScopeBuilder RequirePermission();
    IPolicyScopeBuilder DenyCrossTenant();

    IConditionalPolicyBuilder When(Func<AccessContext, bool> predicate);
}
