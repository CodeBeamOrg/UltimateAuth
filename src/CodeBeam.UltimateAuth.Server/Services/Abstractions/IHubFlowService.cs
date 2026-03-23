using CodeBeam.UltimateAuth.Server.Contracts;

namespace CodeBeam.UltimateAuth.Server.Services;

public interface IHubFlowService
{
    Task<HubSessionResult> BeginLoginAsync(HubBeginRequest request, CancellationToken ct = default);

    Task ContinuePkceAsync(string hubSessionId, string authorizationCode, string codeVerifier, CancellationToken ct = default);

    Task ConsumeAsync(string hubSessionId, CancellationToken ct = default);
}
