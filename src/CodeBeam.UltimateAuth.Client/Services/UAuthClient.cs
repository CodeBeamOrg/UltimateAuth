using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Authentication;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Client
{
    internal sealed class UAuthClient : IUAuthClient
    {
        private readonly IBrowserPostClient _post;
        private readonly UAuthClientOptions _options;
        private readonly UAuthOptions _coreOptions;
        private readonly UAuthClientDiagnostics _diagnostics;
        private readonly NavigationManager _nav;

        public UAuthClient(
            IBrowserPostClient post,
            IOptions<UAuthClientOptions> options,
            IOptions<UAuthOptions> coreOptions,
            UAuthClientDiagnostics diagnostics,
            NavigationManager nav)
        {
            _post = post;
            _options = options.Value;
            _coreOptions = coreOptions.Value;
            _diagnostics = diagnostics;
            _nav = nav;
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
            var raw = await _post.FetchPostJsonRawAsync(url);

            if (!raw.Ok || raw.Body is null)
            {
                return new AuthValidationResult
                {
                    IsValid = false,
                    State = "transport"
                };
            }

            var body = raw.Body.Value.Deserialize<AuthValidationResult>(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return body ?? new AuthValidationResult
            {
                IsValid = false,
                State = "deserialize"
            };
        }

        public async Task BeginPkceAsync(string? returnUrl = null)
        {
            var pkce = _options.Login.Pkce;

            if (!pkce.Enabled)
                throw new InvalidOperationException("PKCE login is disabled by configuration.");

            var verifier = CreateVerifier();
            var challenge = CreateChallenge(verifier);

            var authorizeUrl = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, _options.Endpoints.PkceAuthorize);

            var raw = await _post.FetchPostJsonRawAsync(
                authorizeUrl,
                new Dictionary<string, string>
                {
                    ["code_challenge"] = challenge,
                    ["challenge_method"] = "S256"
                });

            if (!raw.Ok || raw.Body is null)
                throw new InvalidOperationException("PKCE authorize failed.");

            var response = raw.Body.Value.Deserialize<PkceAuthorizeResponse>(
                new JsonSerializerOptions{ PropertyNameCaseInsensitive = true });

            if (response is null || string.IsNullOrWhiteSpace(response.AuthorizationCode))
                throw new InvalidOperationException("Invalid PKCE authorize response.");

            if (pkce.OnAuthorized is not null)
                await pkce.OnAuthorized(response);

            var resolvedReturnUrl = returnUrl
                ?? pkce.ReturnUrl
                ?? _options.Login.DefaultReturnUrl
                ?? _nav.Uri;

            if (pkce.AutoRedirect)
            {
                await NavigateToHubLoginAsync(response.AuthorizationCode, verifier, resolvedReturnUrl);
            }
        }

        public async Task CompletePkceLoginAsync(PkceLoginRequest request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            var url = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, _options.Endpoints.PkceComplete);

            var payload = new Dictionary<string, string>
            {
                ["authorization_code"] = request.AuthorizationCode,
                ["code_verifier"] = request.CodeVerifier,
                ["return_url"] = request.ReturnUrl,

                ["Identifier"] = request.Identifier ?? string.Empty,
                ["Secret"] = request.Secret ?? string.Empty
            };

            await _post.NavigatePostAsync(url, payload);
        }

        private Task NavigateToHubLoginAsync(string authorizationCode, string codeVerifier, string returnUrl)
        {
            var hubLoginUrl = UAuthUrlBuilder.Combine(_options.Endpoints.Authority, _options.Endpoints.HubLoginPath);

            var data = new Dictionary<string, string>
            {
                ["authorization_code"] = authorizationCode,
                ["code_verifier"] = codeVerifier,
                ["return_url"] = returnUrl,
                ["client_profile"] = _coreOptions.ClientProfile.ToString()
            };

            return _post.NavigatePostAsync(hubLoginUrl, data);
        }


        // ---------------- PKCE CRYPTO ----------------

        private static string CreateVerifier()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncode(bytes);
        }

        private static string CreateChallenge(string verifier)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
            return Base64UrlEncode(hash);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

    }
}
