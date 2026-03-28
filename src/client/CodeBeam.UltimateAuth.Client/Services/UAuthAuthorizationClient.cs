using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Services;

internal sealed class UAuthAuthorizationClient : IAuthorizationClient
{
    private readonly IUAuthRequestClient _request;
    private readonly IUAuthClientEvents _events;
    private readonly UAuthClientOptions _options;

    public UAuthAuthorizationClient(IUAuthRequestClient request, IUAuthClientEvents events, IOptions<UAuthClientOptions> options)
    {
        _request = request;
        _events = events;
        _options = options.Value;
    }

    private string Url(string path) => UAuthUrlBuilder.Build(_options.Endpoints.BasePath, path, _options.MultiTenant);

    public async Task<UAuthResult<AuthorizationResult>> CheckAsync(AuthorizationCheckRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/authorization/check"), request);
        return UAuthResultMapper.FromJson<AuthorizationResult>(raw);
    }

    public async Task<UAuthResult<UserRolesResponse>> GetMyRolesAsync(PageRequest? request = null)
    {
        request ??= new PageRequest();
        var raw = await _request.SendJsonAsync(Url("/me/authorization/roles/get"), request);
        return UAuthResultMapper.FromJson<UserRolesResponse>(raw);
    }

    public async Task<UAuthResult<UserRolesResponse>> GetUserRolesAsync(UserKey userKey, PageRequest? request = null)
    {
        request ??= new PageRequest();
        var raw = await _request.SendJsonAsync(Url($"/admin/authorization/users/{userKey}/roles/get"), request);
        return UAuthResultMapper.FromJson<UserRolesResponse>(raw);
    }

    public async Task<UAuthResult> AssignRoleToUserAsync(AssignRoleRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/authorization/users/{request.UserKey.Value}/roles/assign"), request.RoleName);

        var result = UAuthResultMapper.From(raw);

        if (result.IsSuccess)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.AuthorizationChanged, _options.StateEvents.HandlingMode));
        }

        return result;
    }

    public async Task<UAuthResult> RemoveRoleFromUserAsync(RemoveRoleRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/authorization/users/{request.UserKey.Value}/roles/remove"), request.RoleName);

        var result = UAuthResultMapper.From(raw);

        if (result.IsSuccess)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.AuthorizationChanged, _options.StateEvents.HandlingMode));
        }

        return result;
    }

    public async Task<UAuthResult<RoleInfo>> CreateRoleAsync(CreateRoleRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/admin/authorization/roles/create"), request);
        return UAuthResultMapper.FromJson<RoleInfo>(raw);
    }

    public async Task<UAuthResult<PagedResult<RoleInfo>>> QueryRolesAsync(RoleQuery request)
    {
        var raw = await _request.SendJsonAsync(Url("/admin/authorization/roles/query"), request);
        return UAuthResultMapper.FromJson<PagedResult<RoleInfo>>(raw);
    }

    public async Task<UAuthResult> RenameRoleAsync(RenameRoleRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/authorization/roles/{request.Id.Value}/rename"), request);
        var result = UAuthResultMapper.From(raw);

        if (result.IsSuccess)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.AuthorizationChanged, _options.StateEvents.HandlingMode));
        }

        return result;
    }

    public async Task<UAuthResult> SetRolePermissionsAsync(SetRolePermissionsRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/authorization/roles/{request.RoleId.Value}/permissions"), request);
        var result = UAuthResultMapper.From(raw);

        if (result.IsSuccess)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.AuthorizationChanged, _options.StateEvents.HandlingMode));
        }

        return result;
    }

    public async Task<UAuthResult<DeleteRoleResult>> DeleteRoleAsync(DeleteRoleRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/authorization/roles/{request.Id.Value}/delete"), request);
        var result = UAuthResultMapper.FromJson<DeleteRoleResult>(raw);

        if (result.IsSuccess)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.AuthorizationChanged, _options.StateEvents.HandlingMode));
        }

        return result;
    }
}
