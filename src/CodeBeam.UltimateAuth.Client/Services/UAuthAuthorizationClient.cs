using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Services;

internal sealed class UAuthAuthorizationClient : IAuthorizationClient
{
    private readonly IUAuthRequestClient _request;
    private readonly UAuthClientOptions _options;

    public UAuthAuthorizationClient(IUAuthRequestClient request, IOptions<UAuthClientOptions> options)
    {
        _request = request;
        _options = options.Value;
    }

    private string Url(string path) => UAuthUrlBuilder.Build(_options.Endpoints.BasePath, path, _options.MultiTenant);

    public async Task<UAuthResult<AuthorizationResult>> CheckAsync(AuthorizationCheckRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/authorization/check"), request);
        return UAuthResultMapper.FromJson<AuthorizationResult>(raw);
    }

    public async Task<UAuthResult<UserRolesResponse>> GetMyRolesAsync()
    {
        var raw = await _request.SendFormForJsonAsync(Url("/authorization/users/me/roles/get"));
        return UAuthResultMapper.FromJson<UserRolesResponse>(raw);
    }

    public async Task<UAuthResult<UserRolesResponse>> GetUserRolesAsync(UserKey userKey)
    {
        var raw = await _request.SendFormForJsonAsync(Url($"/admin/authorization/users/{userKey}/roles/get"));
        return UAuthResultMapper.FromJson<UserRolesResponse>(raw);
    }

    public async Task<UAuthResult> AssignRoleAsync(UserKey userKey, string role)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/authorization/users/{userKey}/roles/post"), new AssignRoleRequest
        {
            Role = role
        });

        return UAuthResultMapper.FromStatus(raw);
    }

    public async Task<UAuthResult> RemoveRoleAsync(UserKey userKey, string role)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/authorization/users/{userKey}/roles/delete"), new AssignRoleRequest
        {
            Role = role
        });

        return UAuthResultMapper.FromStatus(raw);
    }
}
