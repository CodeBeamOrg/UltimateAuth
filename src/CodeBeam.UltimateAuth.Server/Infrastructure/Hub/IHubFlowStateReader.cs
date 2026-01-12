namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface IHubFlowStateReader
    {
        Task<HubFlowState> GetStateAsync(string hubKey);
    }
}
