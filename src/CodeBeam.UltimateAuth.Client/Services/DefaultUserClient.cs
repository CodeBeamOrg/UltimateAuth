using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Services;

internal sealed class DefaultUserClient : IUserClient
{
    private readonly IUAuthRequestClient _request;
    private readonly UAuthClientOptions _options;

    public DefaultUserClient(IUAuthRequestClient request, IOptions<UAuthClientOptions> options)
    {
        _request = request;
        _options = options.Value;
    }

    public async Task<UAuthResult<UserViewDto>> GetMeAsync()
    {
        var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, "/users/me/get");
        var raw = await _request.SendFormForJsonAsync(url);
        return UAuthResultMapper.FromJson<UserViewDto>(raw);
    }

    public async Task<UAuthResult> UpdateMeAsync(UpdateProfileRequest request)
    {
        var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, "/users/me/update");
        var raw = await _request.SendJsonAsync(url, request);
        return UAuthResultMapper.FromStatus(raw);
    }

    public async Task<UAuthResult<UserCreateResult>> CreateAsync(CreateUserRequest request)
    {
        var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, "/users/create");
        var raw = await _request.SendJsonAsync(url, request);
        return UAuthResultMapper.FromJson<UserCreateResult>(raw);
    }

    public async Task<UAuthResult<UserStatusChangeResult>> ChangeStatusSelfAsync(ChangeUserStatusSelfRequest request)
    {
        var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/users/me/status");
        var raw = await _request.SendJsonAsync(url, request);
        return UAuthResultMapper.FromJson<UserStatusChangeResult>(raw);
    }

    public async Task<UAuthResult<UserStatusChangeResult>> ChangeStatusAdminAsync(ChangeUserStatusAdminRequest request)
    {
        var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/admin/users/{request.UserKey.Value}/status");
        var raw = await _request.SendJsonAsync(url, request);
        return UAuthResultMapper.FromJson<UserStatusChangeResult>(raw);
    }

    public async Task<UAuthResult<UserDeleteResult>> DeleteAsync(DeleteUserRequest request)
    {
        var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, "/users/delete");
        var raw = await _request.SendJsonAsync(url, request);
        return UAuthResultMapper.FromJson<UserDeleteResult>(raw);
    }

    public async Task<UAuthResult<UserViewDto>> GetProfileAsync(UserKey userKey)
    {
        var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/admin/users/{userKey}/profile/get");
        var raw = await _request.SendFormForJsonAsync(url);
        return UAuthResultMapper.FromJson<UserViewDto>(raw);
    }

    public async Task<UAuthResult> UpdateProfileAsync(UserKey userKey, UpdateProfileRequest request)
    {
        var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/admin/users/{userKey}/profile/update");
        var raw = await _request.SendJsonAsync(url, request);
        return UAuthResultMapper.FromStatus(raw);
    }
}
