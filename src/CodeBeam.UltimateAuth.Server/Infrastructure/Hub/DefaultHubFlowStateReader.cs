using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Stores;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class DefaultHubFlowStateReader : IHubFlowStateReader
    {
        private readonly IAuthStore _store;
        private readonly IClock _clock;

        public DefaultHubFlowStateReader(IAuthStore store, IClock clock)
        {
            _store = store;
            _clock = clock;
        }

        public async Task<HubFlowState> GetStateAsync(string hubKey)
        {
            var artifact = await _store.GetAsync(new AuthArtifactKey(hubKey));

            if (artifact is not HubFlowArtifact flow)
                return new HubFlowState(false, false, false, false, null, null, null);

            var expired = flow.IsExpired(_clock.UtcNow);
            var completed = flow.IsCompleted;

            return new HubFlowState(
                Exists: true,
                IsActive: !expired && !completed,
                IsCompleted: completed,
                IsExpired: expired,
                FlowType: flow.FlowType,
                ClientProfile: flow.ClientProfile,
                ReturnUrl: flow.ReturnUrl
            );
        }
    }
}
