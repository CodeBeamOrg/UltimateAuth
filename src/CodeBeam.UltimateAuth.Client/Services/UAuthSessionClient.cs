using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Services;

internal sealed class UAuthSessionClient : ISessionClient
{
    private readonly IUAuthRequestClient _request;
    private readonly UAuthClientOptions _options;
    private readonly IUAuthClientEvents _events;

    public UAuthSessionClient(IUAuthRequestClient request, IOptions<UAuthClientOptions> options, IUAuthClientEvents events)
    {
        _request = request;
        _options = options.Value;
        _events = events;
    }

    private string Url(string path)
        => UAuthUrlBuilder.Build(_options.Endpoints.BasePath, path, _options.MultiTenant);


    public async Task<UAuthResult<PagedResult<SessionChainSummaryDto>>> GetMyChainsAsync(PageRequest? request = null)
    {
        request ??= new PageRequest();
        var raw = await _request.SendJsonAsync(Url("/session/me/chains"), request);
        return UAuthResultMapper.FromJson<PagedResult<SessionChainSummaryDto>>(raw);
    }

    public async Task<UAuthResult<SessionChainDetailDto>> GetMyChainDetailAsync(SessionChainId chainId)
    {
        var raw = await _request.SendFormAsync(Url($"/session/me/chains/{chainId}"));
        return UAuthResultMapper.FromJson<SessionChainDetailDto>(raw);
    }

    public async Task<UAuthResult<RevokeResult>> RevokeMyChainAsync(SessionChainId chainId)
    {
        var raw = await _request.SendJsonAsync(Url($"/session/me/chains/{chainId}/revoke"));
        var result = UAuthResultMapper.FromJson<RevokeResult>(raw);

        if (result.Value?.CurrentChain == true)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.SessionRevoked, _options.UAuthStateRefreshMode));
        }

        return result;
    }

    public async Task<UAuthResult> RevokeMyOtherChainsAsync()
    {
        var raw = await _request.SendFormAsync(Url("/session/me/revoke-others"));
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> RevokeAllMyChainsAsync()
    {
        var raw = await _request.SendFormAsync(Url("/session/me/revoke-all"));
        var result = UAuthResultMapper.From(raw);

        if (result.IsSuccess)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.SessionRevoked, _options.UAuthStateRefreshMode));
        }

        return result;
    }


    public async Task<UAuthResult<PagedResult<SessionChainSummaryDto>>> GetUserChainsAsync(UserKey userKey)
    {
        var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey}/sessions/chains"));
        return UAuthResultMapper.FromJson<PagedResult<SessionChainSummaryDto>>(raw);
    }

    public async Task<UAuthResult<SessionChainDetailDto>> GetUserChainDetailAsync(UserKey userKey, SessionChainId chainId)
    {
        var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey}/sessions/chains/{chainId}"));
        return UAuthResultMapper.FromJson<SessionChainDetailDto>(raw);
    }

    public async Task<UAuthResult> RevokeUserSessionAsync(UserKey userKey, AuthSessionId sessionId)
    {
        var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey}/sessions/{sessionId}/revoke"));
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> RevokeUserChainAsync(UserKey userKey, SessionChainId chainId)
    {
        var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey}/sessions/chains/{chainId}/revoke"));
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> RevokeUserRootAsync(UserKey userKey)
    {
        var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey}/sessions/revoke-root"));
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> RevokeAllUserChainsAsync(UserKey userKey)
    {
        var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey}/sessions/revoke-all"));
        return UAuthResultMapper.From(raw);
    }
}
