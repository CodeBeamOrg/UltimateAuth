using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Stores;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Services;

internal sealed class HubFlowService : IHubFlowService
{
    private readonly IAuthStore _authStore;
    private readonly IClock _clock;
    private readonly UAuthServerOptions _options;

    public HubFlowService(
        IAuthStore authStore,
        IClock clock,
        IOptions<UAuthServerOptions> options)
    {
        _authStore = authStore;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<HubSessionResult> BeginLoginAsync(HubBeginRequest request, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.PreviousHubSessionId))
        {
            await _authStore.ConsumeAsync(new AuthArtifactKey(request.PreviousHubSessionId), ct);
        }

        var hubSessionId = HubSessionId.New();

        var payload = new HubFlowPayload();
        payload.Set("authorization_code", request.AuthorizationCode);
        payload.Set("code_verifier", request.CodeVerifier);

        var artifact = new HubFlowArtifact(
            hubSessionId,
            HubFlowType.Login,
            request.ClientProfile,
            request.Tenant,
            request.Device,
            request.ReturnUrl,
            payload,
            _clock.UtcNow.Add(_options.Hub.FlowLifetime));

        await _authStore.StoreAsync(new AuthArtifactKey(hubSessionId.Value), artifact, ct);

        return new HubSessionResult
        {
            HubSessionId = hubSessionId.Value
        };
    }

    public async Task ContinuePkceAsync(string hubSessionId, string authorizationCode, string codeVerifier, CancellationToken ct = default)
    {
        var key = new AuthArtifactKey(hubSessionId);

        var artifact = await _authStore.GetAsync(key, ct) as HubFlowArtifact;

        if (artifact is null)
            throw new InvalidOperationException("Hub session not found.");

        artifact.Payload.Set("authorization_code", authorizationCode);
        artifact.Payload.Set("code_verifier", codeVerifier);

        artifact.ClearError();

        await _authStore.StoreAsync(key, artifact, ct);
    }

    public async Task ConsumeAsync(string hubSessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(hubSessionId))
            return;

        await _authStore.ConsumeAsync(new AuthArtifactKey(hubSessionId), ct);
    }
}
