using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IHubCredentialResolver
    {
        Task<HubCredentials?> ResolveAsync(HubSessionId hubSessionId, CancellationToken ct = default);
    }
}
