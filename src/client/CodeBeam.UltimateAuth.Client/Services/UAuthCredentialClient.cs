using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Services;

internal sealed class UAuthCredentialClient : ICredentialClient
{
    private readonly IUAuthRequestClient _request;
    private readonly IUAuthClientEvents _events;
    private readonly UAuthClientOptions _options;

    public UAuthCredentialClient(IUAuthRequestClient request, IUAuthClientEvents events, IOptions<UAuthClientOptions> options)
    {
        _request = request;
        _events = events;
        _options = options.Value;
    }

    private string Url(string path) => UAuthUrlBuilder.Build(_options.Endpoints.BasePath, path, _options.MultiTenant);

    public async Task<UAuthResult<CredentialAddResult>> AddMyAsync(AddCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/credentials/add"), request);
        return UAuthResultMapper.FromJson<CredentialAddResult>(raw);
    }

    public async Task<UAuthResult<CredentialChangeResult>> ChangeMyAsync(ChangeCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/credentials/change"), request);
        var result = UAuthResultMapper.FromJson<CredentialChangeResult>(raw);

        if (result.IsSuccess)
        {
            await _events.PublishAsync(
                new UAuthStateEventArgsEmpty(UAuthStateEvent.CredentialsChangedSelf, _options.StateEvents.HandlingMode));
        }

        return result;
    }

    public async Task<UAuthResult> RevokeMyAsync(RevokeCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/credentials/revoke"), request);
        var result = UAuthResultMapper.From(raw);

        if (result.IsSuccess)
        {
            await _events.PublishAsync(
                new UAuthStateEventArgsEmpty(UAuthStateEvent.CredentialsChanged, _options.StateEvents.HandlingMode));
        }

        return result;
    }

    public async Task<UAuthResult<CredentialBeginResetResult>> BeginResetAsync(BeginResetCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/me/credentials/reset/begin"), request);
        return UAuthResultMapper.FromJson<CredentialBeginResetResult>(raw);
    }

    public async Task<UAuthResult<CredentialActionResult>> CompleteResetAsync(CompleteResetCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/credentials/reset/complete"), request);

        var result = UAuthResultMapper.FromJson<CredentialActionResult>(raw);

        if (result.IsSuccess)
        {
            await _events.PublishAsync(
                new UAuthStateEventArgsEmpty(UAuthStateEvent.CredentialsChanged, _options.StateEvents.HandlingMode));
        }

        return result;
    }

    public async Task<UAuthResult<CredentialAddResult>> AddUserAsync(UserKey userKey, AddCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey.Value}/credentials/add"), request);
        return UAuthResultMapper.FromJson<CredentialAddResult>(raw);
    }

    public async Task<UAuthResult<CredentialChangeResult>> ChangeUserAsync(UserKey userKey, ChangeCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey.Value}/credentials/change"), request);
        return UAuthResultMapper.FromJson<CredentialChangeResult>(raw);
    }

    public async Task<UAuthResult> RevokeUserAsync(UserKey userKey, RevokeCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey.Value}/credentials/revoke"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult<CredentialBeginResetResult>> BeginResetUserAsync(UserKey userKey, BeginResetCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey.Value}/credentials/reset/begin"), request);
        return UAuthResultMapper.FromJson<CredentialBeginResetResult>(raw);
    }

    public async Task<UAuthResult<CredentialActionResult>> CompleteResetUserAsync(UserKey userKey, CompleteResetCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey.Value}/credentials/reset/complete"), request);
        return UAuthResultMapper.FromJson<CredentialActionResult>(raw);
    }

    public async Task<UAuthResult> DeleteUserAsync(UserKey userKey, DeleteCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey.Value}/credentials/delete"), request);
        return UAuthResultMapper.From(raw);
    }
}
