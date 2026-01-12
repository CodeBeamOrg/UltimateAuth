using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Stores;

namespace CodeBeam.UltimateAuth.Server.Infrastructure.Hub
{
    internal sealed class DefaultHubCredentialResolver : IHubCredentialResolver
    {
        private readonly IAuthStore _store;

        public DefaultHubCredentialResolver(IAuthStore store)
        {
            _store = store;
        }

        public async Task<HubCredentials?> ResolveAsync(HubSessionId hubSessionId, CancellationToken ct = default)
        {
            var artifact = await _store.GetAsync(new AuthArtifactKey(hubSessionId.Value), ct);

            if (artifact is not HubFlowArtifact flow)
                return null;

            if (flow.IsCompleted)
                return null;

            if (!flow.Payload.TryGet("authorization_code", out string? authorizationCode))
                return null;

            if (!flow.Payload.TryGet("code_verifier", out string? codeVerifier))
                return null;

            return new HubCredentials
            {
                AuthorizationCode = authorizationCode,
                CodeVerifier = codeVerifier,
                ClientProfile = flow.ClientProfile,
            };
        }
    }
}
