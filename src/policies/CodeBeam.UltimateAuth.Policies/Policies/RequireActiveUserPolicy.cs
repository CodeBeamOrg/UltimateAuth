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

        public bool AppliesTo(AccessContext context)
        {
            if (!context.IsAuthenticated || context.IsSystemActor)
                return false;

            return !AllowedForInactive.Any(prefix => context.Action.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static readonly string[] AllowedForInactive =
        {
            "users.status.change.",
            "credentials.password.reset.",
            "login.",
            "reauth."
        };
    }
}
