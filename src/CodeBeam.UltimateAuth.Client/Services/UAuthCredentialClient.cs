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
    private readonly UAuthClientOptions _options;

    public UAuthCredentialClient(IUAuthRequestClient request, IOptions<UAuthClientOptions> options)
    {
        _request = request;
        _options = options.Value;
    }

    private string Url(string path) => UAuthUrlBuilder.Build(_options.Endpoints.BasePath, path, _options.MultiTenant);

    public async Task<UAuthResult<GetCredentialsResult>> GetMyAsync()
    {
        var raw = await _request.SendFormAsync(Url("/credentials/get"));
        return UAuthResultMapper.FromJson<GetCredentialsResult>(raw);
    }

    public async Task<UAuthResult<AddCredentialResult>> AddMyAsync(AddCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url("/credentials/add"), request);
        return UAuthResultMapper.FromJson<AddCredentialResult>(raw);
    }

    public async Task<UAuthResult<ChangeCredentialResult>> ChangeMyAsync(ChangeCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/credentials/change"), request);
        return UAuthResultMapper.FromJson<ChangeCredentialResult>(raw);
    }

    public async Task<UAuthResult> RevokeMyAsync(RevokeCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/credentials/revoke"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> BeginResetMyAsync(BeginCredentialResetRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/credentials/reset/begin"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> CompleteResetMyAsync(CompleteCredentialResetRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/credentials/reset/complete"), request);
        return UAuthResultMapper.From(raw);
    }


    public async Task<UAuthResult<GetCredentialsResult>> GetUserAsync(UserKey userKey)
    {
        var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey}/credentials/get"));
        return UAuthResultMapper.FromJson<GetCredentialsResult>(raw);
    }

    public async Task<UAuthResult<AddCredentialResult>> AddUserAsync(UserKey userKey, AddCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/credentials/add"), request);
        return UAuthResultMapper.FromJson<AddCredentialResult>(raw);
    }

    public async Task<UAuthResult> RevokeUserAsync(UserKey userKey, RevokeCredentialRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/credentials/revoke"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> ActivateUserAsync(UserKey userKey)
    {
        var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey}/credentials/activate"));
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> BeginResetUserAsync(UserKey userKey, BeginCredentialResetRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/credentials/reset/begin"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> CompleteResetUserAsync(UserKey userKey, CompleteCredentialResetRequest request)
    {
        var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/credentials/reset/complete"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> DeleteUserAsync(UserKey userKey)
    {
        var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey}/credentials/delete"));
        return UAuthResultMapper.From(raw);
    }

}
