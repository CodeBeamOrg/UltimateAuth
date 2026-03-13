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

    public async Task<UAuthResult<AddCredentialResult>> AddMyAsync(AddCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/credentials/add"), request);
        return UAuthResultMapper.FromJson<AddCredentialResult>(raw);
    }

    public async Task<UAuthResult<ChangeCredentialResult>> ChangeMyAsync(ChangeCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/me/credentials/change"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.CredentialsChangedSelf, _options.StateEvents.HandlingMode));
        }
        return UAuthResultMapper.FromJson<ChangeCredentialResult>(raw);
    }

    public async Task<UAuthResult> RevokeMyAsync(RevokeCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/me/credentials/revoke"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.CredentialsChanged, _options.StateEvents.HandlingMode));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult<BeginCredentialResetResult>> BeginResetMyAsync(BeginCredentialResetRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/me/credentials/reset/begin"), request);
        return UAuthResultMapper.FromJson<BeginCredentialResetResult>(raw);
    }

    public async Task<UAuthResult<CredentialActionResult>> CompleteResetMyAsync(CompleteCredentialResetRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/me/credentials/reset/complete"), request);
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.CredentialsChanged, _options.StateEvents.HandlingMode));
        }
        return UAuthResultMapper.FromJson<CredentialActionResult>(raw);
    }


    public async Task<UAuthResult<AddCredentialResult>> AddCredentialAsync(UserKey userKey, AddCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/credentials/add"), request);
        return UAuthResultMapper.FromJson<AddCredentialResult>(raw);
    }

    public async Task<UAuthResult<ChangeCredentialResult>> ChangeCredentialAsync(UserKey userKey, ChangeCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/credentials/change"), request);
        return UAuthResultMapper.FromJson<ChangeCredentialResult>(raw);
    }

    public async Task<UAuthResult> RevokeCredentialAsync(UserKey userKey, RevokeCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/credentials/revoke"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult<BeginCredentialResetResult>> BeginResetCredentialAsync(UserKey userKey, BeginCredentialResetRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/credentials/reset/begin"), request);
        return UAuthResultMapper.FromJson<BeginCredentialResetResult>(raw);
    }

    public async Task<UAuthResult<CredentialActionResult>> CompleteResetCredentialAsync(UserKey userKey, CompleteCredentialResetRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/credentials/reset/complete"), request);
        return UAuthResultMapper.FromJson<CredentialActionResult>(raw);
    }

    public async Task<UAuthResult> DeleteCredentialAsync(UserKey userKey)
    {
        var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey}/credentials/delete"));
        return UAuthResultMapper.From(raw);
    }
}
