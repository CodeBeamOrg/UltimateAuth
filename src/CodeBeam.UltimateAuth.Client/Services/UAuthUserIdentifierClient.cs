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

    public async Task<UAuthResult<PagedResult<UserIdentifierDto>>> GetMyIdentifiersAsync(PageRequest? request = null)
    {
        request ??= new PageRequest();
        var raw = await _request.SendJsonAsync(Url("/users/me/identifiers/get"), request);
        return UAuthResultMapper.FromJson<PagedResult<UserIdentifierDto>>(raw);
    }

    public async Task<UAuthResult> AddSelfAsync(AddUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/users/me/identifiers/add"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgs<AddUserIdentifierRequest>(UAuthStateEvent.IdentifiersChanged, _options.StateEvents.HandlingMode, request));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> UpdateSelfAsync(UpdateUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/users/me/identifiers/update"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgs<UpdateUserIdentifierRequest>(UAuthStateEvent.IdentifiersChanged, _options.StateEvents.HandlingMode, request));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> SetPrimarySelfAsync(SetPrimaryUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/users/me/identifiers/set-primary"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.IdentifiersChanged, _options.StateEvents.HandlingMode));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> UnsetPrimarySelfAsync(UnsetPrimaryUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/users/me/identifiers/unset-primary"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.IdentifiersChanged, _options.StateEvents.HandlingMode));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> VerifySelfAsync(VerifyUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/users/me/identifiers/verify"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.IdentifiersChanged, _options.StateEvents.HandlingMode));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> DeleteSelfAsync(DeleteUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/users/me/identifiers/delete"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.IdentifiersChanged, _options.StateEvents.HandlingMode));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult<PagedResult<UserIdentifierDto>>> GetUserIdentifiersAsync(UserKey userKey, PageRequest? request = null)
    {
        request ??= new PageRequest();
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey.Value}/identifiers/get"), request);
        return UAuthResultMapper.FromJson<PagedResult<UserIdentifierDto>>(raw);
    }

    public async Task<UAuthResult> AddAdminAsync(UserKey userKey, AddUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/identifiers/add"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> UpdateAdminAsync(UserKey userKey, UpdateUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/identifiers/update"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> SetPrimaryAdminAsync(UserKey userKey, SetPrimaryUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/identifiers/set-primary"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> UnsetPrimaryAdminAsync(UserKey userKey, UnsetPrimaryUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/identifiers/unset-primary"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> VerifyAdminAsync(UserKey userKey, VerifyUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/identifiers/verify"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> DeleteAdminAsync(UserKey userKey, DeleteUserIdentifierRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/identifiers/delete"), request);
        return UAuthResultMapper.From(raw);
    }
}
