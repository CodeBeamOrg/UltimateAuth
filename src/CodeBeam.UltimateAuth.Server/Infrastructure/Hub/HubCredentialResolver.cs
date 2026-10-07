using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Stores;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class HubCredentialResolver : IHubCredentialResolver
{
    private readonly IAuthStore _store;
    private readonly IClock _clock;

    public HubCredentialResolver(IAuthStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<HubCredentials?> ResolveAsync(HubSessionId hubSessionId, CancellationToken ct = default)
    {
        var artifact = await _store.GetAsync(new AuthArtifactKey(hubSessionId.Value), ct);

        if (artifact is not HubFlowArtifact flow)
            return null;

        if (flow.IsExpired(_clock.UtcNow))
            return null;

        if (flow.IsCompleted)
            return null;

        if (!flow.Payload.TryGet("authorization_code", out string? authorizationCode) || string.IsNullOrWhiteSpace(authorizationCode))
            return null;

        if (!flow.Payload.TryGet("code_verifier", out string? codeVerifier) || string.IsNullOrWhiteSpace(codeVerifier))
            return null;

        return new HubCredentials
        {
            AuthorizationCode = authorizationCode,
            CodeVerifier = codeVerifier,
            ClientProfile = flow.ClientProfile,
        };
    }
}
