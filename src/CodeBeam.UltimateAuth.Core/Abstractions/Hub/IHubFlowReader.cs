using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IHubFlowReader
    {
        Task<HubFlowState?> GetStateAsync(HubSessionId hubSessionId, CancellationToken ct = default);
    }
}
