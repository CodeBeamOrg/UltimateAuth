using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal sealed class NoOpHubCredentialResolver : IHubCredentialResolver
{
    public Task<HubCredentials?> ResolveAsync(HubSessionId sessionId, CancellationToken ct = default) => Task.FromResult<HubCredentials?>(null);
}
