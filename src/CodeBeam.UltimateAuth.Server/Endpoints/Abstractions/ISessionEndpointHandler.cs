using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface ISessionEndpointHandler
{
    Task<IResult> GetMyChainsAsync(HttpContext ctx);
    Task<IResult> GetMyChainDetailAsync(SessionChainId chainId, HttpContext ctx);
    Task<IResult> RevokeMyChainAsync(SessionChainId chainId, HttpContext ctx);
    Task<IResult> RevokeOtherChainsAsync(HttpContext ctx);
    Task<IResult> RevokeAllMyChainsAsync(HttpContext ctx);

    Task<IResult> GetUserChainsAsync(UserKey userKey, HttpContext ctx);
    Task<IResult> GetUserChainDetailAsync(UserKey userKey, SessionChainId chainId, HttpContext ctx);
    Task<IResult> RevokeUserSessionAsync(UserKey userKey, AuthSessionId sessionId, HttpContext ctx);
    Task<IResult> RevokeUserChainAsync(UserKey userKey, SessionChainId chainId, HttpContext ctx);
    Task<IResult> RevokeAllChainsAsync(UserKey userKey, SessionChainId? exceptChainId, HttpContext ctx);
    Task<IResult> RevokeRootAsync(UserKey userKey, HttpContext ctx);
}
