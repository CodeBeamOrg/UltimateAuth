using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Services
{
    public class DefaultUserIdentifierClient : IUserIdentifierClient
    {
        private readonly IUAuthRequestClient _request;
        private readonly UAuthClientOptions _options;

        public DefaultUserIdentifierClient(IUAuthRequestClient request, IOptions<UAuthClientOptions> options)
        {
            _request = request;
            _options = options.Value;
        }

        public async Task<UAuthResult<IReadOnlyList<UserIdentifierDto>>> GetMyIdentifiersAsync()
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, "/users/me/identifiers/get");
            var raw = await _request.SendFormForJsonAsync(url);
            return UAuthResultMapper.FromJson<IReadOnlyList<UserIdentifierDto>>(raw);
        }

        public async Task<UAuthResult> AddSelfAsync(AddUserIdentifierRequest request)
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/users/me/identifiers/add");
            var raw = await _request.SendJsonAsync(url, request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> UpdateSelfAsync(UpdateUserIdentifierRequest request)
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/users/me/identifiers/update");
            var raw = await _request.SendJsonAsync(url, request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> SetPrimarySelfAsync(SetPrimaryUserIdentifierRequest request)
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/users/me/identifiers/set-primary");
            var raw = await _request.SendJsonAsync(url, request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> UnsetPrimarySelfAsync(UnsetPrimaryUserIdentifierRequest request)
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/users/me/identifiers/unset-primary");
            var raw = await _request.SendJsonAsync(url, request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> VerifySelfAsync(VerifyUserIdentifierRequest request)
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/users/me/identifiers/verify");
            var raw = await _request.SendJsonAsync(url, request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> DeleteSelfAsync(DeleteUserIdentifierRequest request)
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/users/me/identifiers/delete");
            var raw = await _request.SendJsonAsync(url, request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult<IReadOnlyList<UserIdentifierDto>>> GetUserIdentifiersAsync(UserKey userKey)
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/admin/users/{userKey.Value}/identifiers/get");
            var raw = await _request.SendFormForJsonAsync(url);
            return UAuthResultMapper.FromJson<IReadOnlyList<UserIdentifierDto>>(raw);
        }

        public async Task<UAuthResult> AddAdminAsync(UserKey userKey, AddUserIdentifierRequest request)
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/admin/users/{userKey}/identifiers/add");
            var raw = await _request.SendJsonAsync(url, request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> UpdateAdminAsync(UserKey userKey, UpdateUserIdentifierRequest request)
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/admin/users/{userKey}/identifiers/update");
            var raw = await _request.SendJsonAsync(url, request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> SetPrimaryAdminAsync(UserKey userKey, SetPrimaryUserIdentifierRequest request)
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/admin/users/{userKey}/identifiers/set-primary");
            var raw = await _request.SendJsonAsync(url, request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> UnsetPrimaryAdminAsync(UserKey userKey, UnsetPrimaryUserIdentifierRequest request)
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/admin/users/{userKey}/identifiers/unset-primary");
            var raw = await _request.SendJsonAsync(url, request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> VerifyAdminAsync(UserKey userKey, VerifyUserIdentifierRequest request)
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/admin/users/{userKey}/identifiers/verify");
            var raw = await _request.SendJsonAsync(url, request);
            return UAuthResultMapper.FromStatus(raw);
        }

        public async Task<UAuthResult> DeleteAdminAsync(UserKey userKey, DeleteUserIdentifierRequest request)
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, $"/admin/users/{userKey}/identifiers/delete");
            var raw = await _request.SendJsonAsync(url, request);
            return UAuthResultMapper.FromStatus(raw);
        }

    }
}
