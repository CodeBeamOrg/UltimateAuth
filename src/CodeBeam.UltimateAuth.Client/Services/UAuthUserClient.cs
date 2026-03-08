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

    public async Task<UAuthResult<UserViewDto>> GetMeAsync()
    {
        var raw = await _request.SendFormAsync(Url("/users/me/get"));
        return UAuthResultMapper.FromJson<UserViewDto>(raw);
    }

    public async Task<UAuthResult> UpdateMeAsync(UpdateProfileRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/users/me/update"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgs<UpdateProfileRequest>(UAuthStateEvent.ProfileChanged, _options.UAuthStateRefreshMode, request));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult<UserCreateResult>> CreateAsync(CreateUserRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/users/create"), request);
        return UAuthResultMapper.FromJson<UserCreateResult>(raw);
    }

    public async Task<UAuthResult<UserStatusChangeResult>> ChangeStatusSelfAsync(ChangeUserStatusSelfRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/users/me/status"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgs<ChangeUserStatusSelfRequest>(UAuthStateEvent.ProfileChanged, _options.UAuthStateRefreshMode, request));
        }
        return UAuthResultMapper.FromJson<UserStatusChangeResult>(raw);
    }

    public async Task<UAuthResult<UserStatusChangeResult>> ChangeStatusAdminAsync(ChangeUserStatusAdminRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{request.UserKey.Value}/status"), request);
        return UAuthResultMapper.FromJson<UserStatusChangeResult>(raw);
    }

    public async Task<UAuthResult<UserDeleteResult>> DeleteAsync(DeleteUserRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/users/delete"));
        return UAuthResultMapper.FromJson<UserDeleteResult>(raw);
    }

    public async Task<UAuthResult<UserViewDto>> GetProfileAsync(UserKey userKey)
    {
        var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey}/profile/get"));
        return UAuthResultMapper.FromJson<UserViewDto>(raw);
    }

    public async Task<UAuthResult> UpdateProfileAsync(UserKey userKey, UpdateProfileRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/profile/update"), request);
        return UAuthResultMapper.From(raw);
    }
}
