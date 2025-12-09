using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Models;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface ISessionService<TUserId>
    {
        Task<SessionResult<TUserId>> CreateLoginSessionAsync(TUserId userId, DeviceInfo deviceInfo, SessionMetadata? metadata, DateTime now);
        Task<SessionResult<TUserId>> RefreshSessionAsync(AuthSessionId currentSessionId,DateTime now);

        Task RevokeSessionAsync(AuthSessionId sessionId, DateTime at);
        Task RevokeChainAsync(ChainId chainId, DateTime at);
        Task RevokeRootAsync(TUserId userId, DateTime at);

        Task<SessionValidationResult<TUserId>> ValidateSessionAsync(AuthSessionId sessionId, DateTime now);

        Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsAsync(TUserId userId);
        Task<IReadOnlyList<ISession<TUserId>>> GetSessionsAsync(ChainId chainId);
    }
}
