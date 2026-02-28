using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Services;

public interface ISessionApplicationService
{
    Task<PagedResult<SessionChainSummaryDto>> GetUserChainsAsync(AccessContext context,UserKey userKey, PageRequest request, CancellationToken ct = default);
    
    Task<SessionChainDetailDto> GetUserChainDetailAsync(AccessContext context, UserKey userKey, SessionChainId chainId, CancellationToken ct = default);

    Task RevokeUserSessionAsync(AccessContext context, UserKey userKey, AuthSessionId sessionId, CancellationToken ct = default);

    Task RevokeUserChainAsync(AccessContext context, UserKey userKey, SessionChainId chainId, CancellationToken ct = default);

    Task RevokeOtherChainsAsync(AccessContext context, UserKey userKey, SessionChainId? currentChainId, CancellationToken ct = default);

    Task RevokeAllChainsAsync(AccessContext context, UserKey userKey, SessionChainId? exceptChainId, CancellationToken ct = default);

    Task RevokeRootAsync(AccessContext context, UserKey userKey, CancellationToken ct = default);
}
