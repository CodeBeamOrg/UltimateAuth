using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Infrastructure
{
    internal sealed class NoOpHubFlowReader : IHubFlowReader
    {
        public Task<HubFlowState?> GetStateAsync(HubSessionId sessionId, CancellationToken ct = default)  => Task.FromResult<HubFlowState?>(null);
    }
}
