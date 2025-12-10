using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Models;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface ISessionService<TUserId>
    {
        Task<SessionResult<TUserId>> CreateLoginSessionAsync(string? tenantId, TUserId userId, DeviceInfo deviceInfo, SessionMetadata? metadata, DateTime now);
        Task<SessionResult<TUserId>> RefreshSessionAsync(string? tenantId, AuthSessionId currentSessionId,DateTime now);

        Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTime at);
        Task RevokeChainAsync(string? tenantId, ChainId chainId, DateTime at);
        Task RevokeRootAsync(string? tenantId, TUserId userId, DateTime at);

        Task<SessionValidationResult<TUserId>> ValidateSessionAsync(string? tenantId, AuthSessionId sessionId, DateTime now);

        Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsAsync(string? tenantId, TUserId userId);
        Task<IReadOnlyList<ISession<TUserId>>> GetSessionsAsync(string? tenantId, ChainId chainId);
    }
}
