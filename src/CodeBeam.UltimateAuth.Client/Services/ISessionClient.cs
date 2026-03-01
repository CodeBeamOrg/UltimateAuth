using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface ISessionClient
{
    Task<UAuthResult<PagedResult<SessionChainSummaryDto>>> GetMyChainsAsync(PageRequest? request = null);
    Task<UAuthResult<SessionChainDetailDto>> GetMyChainDetailAsync(SessionChainId chainId);
    Task<UAuthResult> RevokeMyChainAsync(SessionChainId chainId);
    Task<UAuthResult> RevokeMyOtherChainsAsync();
    Task<UAuthResult> RevokeAllMyChainsAsync();


    Task<UAuthResult<PagedResult<SessionChainSummaryDto>>> GetUserChainsAsync(UserKey userKey);
    Task<UAuthResult<SessionChainDetailDto>> GetUserChainAsync(UserKey userKey, SessionChainId chainId);
    Task<UAuthResult> RevokeUserSessionAsync(UserKey userKey, AuthSessionId sessionId);
    Task<UAuthResult> RevokeUserChainAsync(UserKey userKey, SessionChainId chainId);
    Task<UAuthResult> RevokeUserRootAsync(UserKey userKey);
    Task<UAuthResult> RevokeAllUserChainsAsync(UserKey userKey);
}
