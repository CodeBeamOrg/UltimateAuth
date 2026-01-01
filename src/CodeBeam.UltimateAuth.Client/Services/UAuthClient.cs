using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client
{
    internal sealed class UAuthClient : IUAuthClient
    {
        private readonly IBrowserPostClient _post;
        private readonly UAuthClientOptions _options;
        private readonly UAuthClientDiagnostics _diagnostics;

        public UAuthClient(
            IBrowserPostClient post,
            IOptions<UAuthClientOptions> options,
            UAuthClientDiagnostics diagnostics)
        {
            _post = post;
            _options = options.Value;
            _diagnostics = diagnostics;
        }

        public async Task LoginAsync(LoginRequest request)
        {
            await _post.NavigatePostAsync(_options.Endpoints.Login, request.ToDictionary());
        }

        public async Task LogoutAsync()
        {
            await _post.NavigatePostAsync(_options.Endpoints.Logout);
        }

        public async Task<RefreshResult> RefreshAsync(bool isAuto = false)
        {
            if (isAuto == false)
            {
                _diagnostics.MarkManualRefresh();
            }

            var result = await _post.BackgroundPostAsync(_options.Endpoints.Refresh);
            var refreshOutcome = RefreshOutcomeParser.Parse(result.RefreshOutcome);
            switch (refreshOutcome)
            {
                case RefreshOutcome.NoOp:
                    _diagnostics.MarkRefreshNoOp();
                    break;
                case RefreshOutcome.Touched:
                    _diagnostics.MarkRefreshTouched();
                    break;
                case RefreshOutcome.ReauthRequired:
                    _diagnostics.MarkRefreshReauthRequired();
                    break;
                case RefreshOutcome.None:
                    _diagnostics.MarkRefreshUnknown();
                    break;
            }

            return new RefreshResult
            {
                Ok = result.Ok,
                Status = result.Status,
                Outcome = refreshOutcome
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
