using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class UAuthAccessOrchestrator : IAccessOrchestrator
    {
        private readonly IAccessAuthority _authority;
        private bool _executed;

        public UAuthAccessOrchestrator(IAccessAuthority authority)
        {
            _authority = authority;
        }

        public async Task ExecuteAsync(AccessContext context, IAccessCommand command, CancellationToken ct = default)
        {
            if (_executed)
                throw new InvalidOperationException("Access orchestrator can only be executed once.");

            _executed = true;

            var policies = command.GetPolicies(context) ?? Array.Empty<IAccessPolicy>();
            var decision = _authority.Decide(context, policies);

            if (!decision.IsAllowed)
                throw new UAuthAuthorizationException(decision.DenyReason);

            if (decision.RequiresReauthentication)
                throw new InvalidOperationException("Requires reuthenticate.");

            await command.ExecuteAsync(ct);
        }
    }
}
