using CodeBeam.UltimateAuth.Client.Abstractions;
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
        private readonly IClientAuthState _state;

        public UAuthClient(
            IBrowserPostClient post,
            IOptions<UAuthClientOptions> options,
            UAuthClientDiagnostics diagnostics,
            IClientAuthState state)
        {
            _post = post;
            _options = options.Value;
            _diagnostics = diagnostics;
            _state = state;
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
                State = result.Body.State
            };
        }

        public Task<ClaimsPrincipal> GetCurrentPrincipalAsync()
        {
            if (!_state.IsAuthenticated)
            {
                return Task.FromResult(CreateAnonymous());
            }

            if (_state.Claims == null || _state.Claims.Count == 0)
            {
                return Task.FromResult(CreateAnonymous());
            }

            var identity = new ClaimsIdentity(claims: _state.Claims, authenticationType: "UltimateAuth");
            var principal = new ClaimsPrincipal(identity);

            return Task.FromResult(principal);
        }

        private static ClaimsPrincipal CreateAnonymous()
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

    }
}
