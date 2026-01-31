using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Services
{
    internal sealed class DefaultCredentialClient : ICredentialClient
    {
        private readonly IUAuthRequestClient _request;
        private readonly UAuthClientOptions _options;

        public DefaultCredentialClient(IUAuthRequestClient request, IOptions<UAuthClientOptions> options)
        {
            _request = request;
            _options = options.Value;
        }

        private string Url(string path) => UAuthUrlBuilder.Combine(_options.Endpoints.Authority, path);

        public async Task<UAuthResult<GetCredentialsResult>> GetMyAsync()
        {
            var raw = await _request.SendFormForJsonAsync(Url("/credentials/get"));
            return UAuthResultMapper.FromJson<GetCredentialsResult>(raw);
        }

        public async Task<UAuthResult<AddCredentialResult>> AddMyAsync(AddCredentialRequest request)
        {
            var raw = await _request.SendJsonAsync(Url("/credentials/add"), request);
            return UAuthResultMapper.FromJson<AddCredentialResult>(raw);
        }

        public async Task<UAuthResult<ChangeCredentialResult>> ChangeMyAsync(CredentialType type, ChangeCredentialRequest request)
        {
            var raw = await _request.SendJsonAsync(Url($"/credentials/{type}/change"), request);
            return UAuthResultMapper.FromJson<ChangeCredentialResult>(raw);
        }

        public async Task<UAuthResult> RevokeMyAsync(CredentialType type, RevokeCredentialRequest request)
        {
            var raw = await _request.SendJsonAsync(Url($"/credentials/{type}/revoke"), request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> BeginResetMyAsync(CredentialType type, BeginCredentialResetRequest request)
        {
            var raw = await _request.SendJsonAsync(Url($"/credentials/{type}/reset/begin"), request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> CompleteResetMyAsync(CredentialType type, CompleteCredentialResetRequest request)
        {
            var raw = await _request.SendJsonAsync(Url($"/credentials/{type}/reset/complete"), request);
            return UAuthResultMapper.FromStatus(raw);
        }


        public async Task<UAuthResult<GetCredentialsResult>> GetUserAsync(UserKey userKey)
        {
            var raw = await _request.SendFormForJsonAsync(Url($"/admin/users/{userKey}/credentials/get"));
            return UAuthResultMapper.FromJson<GetCredentialsResult>(raw);
        }

        public async Task<UAuthResult<AddCredentialResult>> AddUserAsync(UserKey userKey, AddCredentialRequest request)
        {
            var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/credentials/add"), request);
            return UAuthResultMapper.FromJson<AddCredentialResult>(raw);
        }

        public async Task<UAuthResult> RevokeUserAsync(UserKey userKey, CredentialType type, RevokeCredentialRequest request)
        {
            var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/credentials/{type}/revoke"), request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> ActivateUserAsync(UserKey userKey, CredentialType type)
        {
            var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey}/credentials/{type}/activate"));
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> BeginResetUserAsync(UserKey userKey, CredentialType type, BeginCredentialResetRequest request)
        {
            var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/credentials/{type}/reset/begin"), request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> CompleteResetUserAsync(UserKey userKey, CredentialType type, CompleteCredentialResetRequest request)
        {
            var raw = await _request.SendJsonAsync(Url($"/admin/users/{userKey}/credentials/{type}/reset/complete"), request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> DeleteUserAsync(UserKey userKey, CredentialType type)
        {
            var raw = await _request.SendFormAsync(Url($"/admin/users/{userKey}/credentials/{type}/delete"));
            return UAuthResultMapper.FromStatus(raw);
        }

    }
}
