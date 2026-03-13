using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Services;

internal sealed class UAuthUserClient : IUserClient
{
    private readonly IUAuthRequestClient _request;
    private readonly IUAuthClientEvents _events;
    private readonly UAuthClientOptions _options;

    public UAuthUserClient(IUAuthRequestClient request, IUAuthClientEvents events, IOptions<UAuthClientOptions> options)
    {
        _request = request;
        _events = events;
        _options = options.Value;
    }

    private string Url(string path) => UAuthUrlBuilder.Build(_options.Endpoints.BasePath, path, _options.MultiTenant);

    public async Task<UAuthResult<UserView>> GetMeAsync()
    {
        var raw = await _request.SendFormAsync(Url("/me/get"));
        return UAuthResultMapper.FromJson<UserView>(raw);
    }

    public async Task<UAuthResult> UpdateMeAsync(UpdateProfileRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/update"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgs<UpdateProfileRequest>(UAuthStateEvent.ProfileChanged, _options.StateEvents.HandlingMode, request));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> DeleteMeAsync()
    {
        var raw = await _request.SendJsonAsync(Url("/me/delete"));
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.UserDeleted, UAuthStateEventHandlingMode.Patch));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult<PagedResult<UserSummary>>> QueryUsersAsync(UserQuery query)
    {
        query ??= new UserQuery();
        var raw = await _request.SendJsonAsync(Url("/admin/users/query"), query);
        return UAuthResultMapper.FromJson<PagedResult<UserSummary>>(raw);
    }

    public async Task<UAuthResult<UserCreateResult>> CreateAsync(CreateUserRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/users/create"), request);
        return UAuthResultMapper.FromJson<UserCreateResult>(raw);
    }

    public async Task<UAuthResult<UserStatusChangeResult>> ChangeStatusSelfAsync(ChangeUserStatusSelfRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/status"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgs<ChangeUserStatusSelfRequest>(UAuthStateEvent.ProfileChanged, _options.StateEvents.HandlingMode, request));
        }
        return UAuthResultMapper.FromJson<UserStatusChangeResult>(raw);
    }

    public async Task<UAuthResult<UserStatusChangeResult>> ChangeStatusAdminAsync(UserKey userKey, ChangeUserStatusAdminRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey.Value}/status"), request);
        return UAuthResultMapper.FromJson<UserStatusChangeResult>(raw);
    }

    public async Task<UAuthResult<UserDeleteResult>> DeleteUserAsync(UserKey userKey, DeleteUserRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey.Value}/delete"), request);
        return UAuthResultMapper.FromJson<UserDeleteResult>(raw);
    }

    public async Task<UAuthResult<UserView>> GetProfileAsync(UserKey userKey)
    {
        var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey.Value}/profile/get"));
        return UAuthResultMapper.FromJson<UserView>(raw);
    }

    public async Task<UAuthResult> UpdateProfileAsync(UserKey userKey, UpdateProfileRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey.Value}/profile/update"), request);
        return UAuthResultMapper.From(raw);
    }
}
