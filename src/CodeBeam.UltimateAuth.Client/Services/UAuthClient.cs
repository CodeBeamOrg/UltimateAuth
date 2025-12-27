using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client
{
    internal sealed class UAuthClient : IUAuthClient
    {
        private readonly IBrowserPostClient _post;
        private readonly UAuthClientOptions _options;

        public UAuthClient(
            IBrowserPostClient post,
            IOptions<UAuthClientOptions> options)
        {
            _post = post;
            _options = options.Value;
        }

        public Task LoginAsync(LoginRequest request)
            => _post.PostAsync(_options.Endpoints.Login, request.ToDictionary());

        public Task LogoutAsync()
            => _post.PostAsync(_options.Endpoints.Logout);

        public Task RefreshAsync()
            => _post.PostAsync(_options.Endpoints.Refresh);

        public Task ReauthAsync()
            => _post.PostAsync(_options.Endpoints.Reauth);

        public Task<AuthValidationResult> ValidateAsync()
        {
            // Blazor Server: direct service
            // WASM: HttpClient
            throw new NotImplementedException();
        }
    }

}
