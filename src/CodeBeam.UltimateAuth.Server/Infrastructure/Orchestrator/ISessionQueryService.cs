using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface ISessionQueryService<TUserId>
    {
        Task<SessionValidationResult<TUserId>> ValidateSessionAsync(SessionValidationContext context, CancellationToken ct = default);

        Task<ISession<TUserId>?> GetSessionAsync(string? tenantId, AuthSessionId sessionId, CancellationToken ct = default);

        Task<IReadOnlyList<ISession<TUserId>>> GetSessionsByChainAsync(string? tenantId, ChainId chainId, CancellationToken ct = default);

        Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsByUserAsync(string? tenantId, TUserId userId, CancellationToken ct = default);

        Task<ChainId?> ResolveChainIdAsync(string? tenantId, AuthSessionId sessionId, CancellationToken ct = default);
    }
}
