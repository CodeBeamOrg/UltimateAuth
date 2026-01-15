using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface ISessionQueryService
    {
        Task<SessionValidationResult> ValidateSessionAsync(SessionValidationContext context, CancellationToken ct = default);

        Task<ISession?> GetSessionAsync(string? tenantId, AuthSessionId sessionId, CancellationToken ct = default);

        Task<IReadOnlyList<ISession>> GetSessionsByChainAsync(string? tenantId, SessionChainId chainId, CancellationToken ct = default);

        Task<IReadOnlyList<ISessionChain>> GetChainsByUserAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);

        Task<SessionChainId?> ResolveChainIdAsync(string? tenantId, AuthSessionId sessionId, CancellationToken ct = default);
    }
}
