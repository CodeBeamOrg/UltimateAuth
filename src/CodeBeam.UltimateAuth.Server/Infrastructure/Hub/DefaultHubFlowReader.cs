using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Stores;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class DefaultHubFlowReader : IHubFlowReader
    {
        private readonly IAuthStore _store;
        private readonly IClock _clock;

        public DefaultHubFlowReader(IAuthStore store, IClock clock)
        {
            _store = store;
            _clock = clock;
        }

        public async Task<HubFlowState?> GetStateAsync(HubSessionId hubSessionId, CancellationToken ct = default)
        {
            var artifact = await _store.GetAsync(new AuthArtifactKey(hubSessionId.Value), ct);

            if (artifact is not HubFlowArtifact flow)
                return null;

            var now = _clock.UtcNow;

            return new HubFlowState
            {
                Exists = true,
                HubSessionId = flow.HubSessionId,
                FlowType = flow.FlowType,
                ClientProfile = flow.ClientProfile,
                ReturnUrl = flow.ReturnUrl,
                IsExpired = flow.IsExpired(now),
                IsCompleted = flow.IsCompleted,
                IsActive = !flow.IsExpired(now) && !flow.IsCompleted
            };
        }
    }

}
