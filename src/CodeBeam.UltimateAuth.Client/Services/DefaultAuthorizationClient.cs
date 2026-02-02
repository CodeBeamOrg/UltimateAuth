using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Services;

internal sealed class DefaultAuthorizationClient : IAuthorizationClient
{
    private readonly IUAuthRequestClient _request;
    private readonly UAuthClientOptions _options;

    public DefaultAuthorizationClient(IUAuthRequestClient request, IOptions<UAuthClientOptions> options)
    {
        _request = request;
        _options = options.Value;
    }

    public async Task<UAuthResult<AuthorizationResult>> CheckAsync(AuthorizationCheckRequest request)
    {
        var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, "/authorization/check");
        var raw = await _request.SendJsonAsync(url, request);
        return UAuthResultMapper.FromJson<AuthorizationResult>(raw);
    }

    public async Task<UAuthResult<UserRolesResponse>> GetMyRolesAsync()
    {
        var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, "/authorization/users/me/roles/get");
        var raw = await _request.SendFormForJsonAsync(url);
        return UAuthResultMapper.FromJson<UserRolesResponse>(raw);
    }

    public async Task<UAuthResult<UserRolesResponse>> GetUserRolesAsync(UserKey userKey)
    {
        var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/admin/authorization/users/{userKey}/roles/get");
        var raw = await _request.SendFormForJsonAsync(url);
        return UAuthResultMapper.FromJson<UserRolesResponse>(raw);
    }

    public async Task<UAuthResult> AssignRoleAsync(UserKey userKey, string role)
    {
        var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/admin/authorization/users/{userKey}/roles/post");
        var raw = await _request.SendJsonAsync(url, new AssignRoleRequest
        {
            Role = role
        });

        return UAuthResultMapper.FromStatus(raw);
    }

    public async Task<UAuthResult> RemoveRoleAsync(UserKey userKey, string role)
    {
        var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/admin/authorization/users/{userKey}/roles/delete");

        var raw = await _request.SendJsonAsync(url, new AssignRoleRequest
        {
            Role = role
        });

        return UAuthResultMapper.FromStatus(raw);
    }
}
