using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies
{
    internal sealed class RequireActiveUserPolicy : IAccessPolicy
    {
        private readonly IUserRuntimeStateProvider _runtime;

        public RequireActiveUserPolicy(IUserRuntimeStateProvider runtime)
        {
            _runtime = runtime;
        }

        public AccessDecision Decide(AccessContext context)
        {
            if (context.ActorUserKey is null)
                return AccessDecision.Deny("missing_actor");

            var state = _runtime.GetAsync(context.ActorTenantId, context.ActorUserKey!.Value).GetAwaiter().GetResult();

            if (state == null || !state.Exists || state.IsDeleted)
                return AccessDecision.Deny("user_not_found");

            return state.IsActive
                ? AccessDecision.Allow()
                : AccessDecision.Deny("user_not_active");
        }

        public bool AppliesTo(AccessContext context) => context.IsAuthenticated && !context.IsSystemActor;
    }
}
