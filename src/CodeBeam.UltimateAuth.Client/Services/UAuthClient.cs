using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Client.Infrastructure;
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

        public async Task LoginAsync(LoginRequest request)
            => await _post.NavigatePostAsync(_options.Endpoints.Login, request.ToDictionary());

        public async Task LogoutAsync()
            => await _post.NavigatePostAsync(_options.Endpoints.Logout);

        public async Task<RefreshResult> RefreshAsync()
        {
            var result = await _post.BackgroundPostAsync(
                _options.Endpoints.Refresh);

            return new RefreshResult
            {
                Ok = result.Ok,
                Status = result.Status,
                Outcome = RefreshOutcomeParser.Parse(result.RefreshOutcome)
            };
        }

        public Task ReauthAsync()
            => _post.NavigatePostAsync(_options.Endpoints.Reauth);

        public Task<AuthValidationResult> ValidateAsync()
        {
            // Blazor Server: direct service
            // WASM: HttpClient
            throw new NotImplementedException();
        }
    }

}
