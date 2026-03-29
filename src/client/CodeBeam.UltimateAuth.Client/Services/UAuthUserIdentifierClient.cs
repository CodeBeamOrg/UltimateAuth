using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Services;

internal class UAuthUserIdentifierClient : IUserIdentifierClient
{
    private readonly IUAuthRequestClient _request;
    private readonly IUAuthClientEvents _events;
    private readonly UAuthClientOptions _options;

    public UAuthUserIdentifierClient(IUAuthRequestClient request, IUAuthClientEvents events, IOptions<UAuthClientOptions> options)
    {
        _request = request;
        _events = events;
        _options = options.Value;
    }

    private string Url(string path) => UAuthUrlBuilder.Build(_options.Endpoints.BasePath, path, _options.MultiTenant);

    public async Task<UAuthResult<PagedResult<UserIdentifierInfo>>> GetMyAsync(PageRequest? request = null)
    {
        request ??= new PageRequest();
        var raw = await _request.SendJsonAsync(Url("/me/identifiers/get"), request);
        return UAuthResultMapper.FromJson<PagedResult<UserIdentifierInfo>>(raw);
    }

    public async Task<UAuthResult> AddMyAsync(AddUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/identifiers/add"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgs<AddUserIdentifierRequest>(UAuthStateEvent.IdentifiersChanged, _options.StateEvents.HandlingMode, request));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> UpdateMyAsync(UpdateUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/identifiers/update"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgs<UpdateUserIdentifierRequest>(UAuthStateEvent.IdentifiersChanged, _options.StateEvents.HandlingMode, request));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> SetMyPrimaryAsync(SetPrimaryUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/identifiers/set-primary"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.IdentifiersChanged, _options.StateEvents.HandlingMode));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> UnsetMyPrimaryAsync(UnsetPrimaryUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/identifiers/unset-primary"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.IdentifiersChanged, _options.StateEvents.HandlingMode));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> VerifyMyAsync(VerifyUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/identifiers/verify"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.IdentifiersChanged, _options.StateEvents.HandlingMode));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> DeleteMyAsync(DeleteUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/identifiers/delete"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.IdentifiersChanged, _options.StateEvents.HandlingMode));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult<PagedResult<UserIdentifierInfo>>> GetUserAsync(UserKey userKey, PageRequest? request = null)
    {
        request ??= new PageRequest();
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey.Value}/identifiers/get"), request);
        return UAuthResultMapper.FromJson<PagedResult<UserIdentifierInfo>>(raw);
    }

    public async Task<UAuthResult> AddUserAsync(UserKey userKey, AddUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/identifiers/add"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> UpdateUserAsync(UserKey userKey, UpdateUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/identifiers/update"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> SetUserPrimaryAsync(UserKey userKey, SetPrimaryUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/identifiers/set-primary"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> UnsetUserPrimaryAsync(UserKey userKey, UnsetPrimaryUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/identifiers/unset-primary"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> VerifyUserAsync(UserKey userKey, VerifyUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/identifiers/verify"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> DeleteUserAsync(UserKey userKey, DeleteUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/identifiers/delete"), request);
        return UAuthResultMapper.From(raw);
    }
}
