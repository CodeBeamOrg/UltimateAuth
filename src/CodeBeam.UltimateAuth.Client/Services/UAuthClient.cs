using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Authentication;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.Extensions.Options;
using System.Security.Claims;

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
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, _options.Endpoints.Login);
            await _post.NavigatePostAsync(url, request.ToDictionary());
        }

        public async Task LogoutAsync()
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, _options.Endpoints.Logout);
            await _post.NavigatePostAsync(url);
        }

        public async Task<RefreshResult> RefreshAsync(bool isAuto = false)
        {
            if (isAuto == false)
            {
                _diagnostics.MarkManualRefresh();
            }

            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, _options.Endpoints.Refresh);
            var result = await _post.FetchPostAsync(url);
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

        public async Task ReauthAsync()
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, _options.Endpoints.Reauth);
            await _post.NavigatePostAsync(_options.Endpoints.Reauth);
        }

        public async Task<AuthValidationResult> ValidateAsync()
        {
            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, _options.Endpoints.Validate);
            var result = await _post.FetchPostJsonAsync<AuthValidationResult>(url);

            if (result.Body is null)
                return new AuthValidationResult { IsValid = false, State = "transport" };

            return new AuthValidationResult
            {
                IsValid = result.Body.IsValid,
                State = result.Body.State,
                RemainingAttempts = result.Body.RemainingAttempts,
                Snapshot = result.Body.Snapshot,
            };
        }

    }
}
