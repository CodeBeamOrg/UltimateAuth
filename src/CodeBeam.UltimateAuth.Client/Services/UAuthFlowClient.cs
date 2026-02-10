using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Client.Services;

internal class UAuthFlowClient : IFlowClient
{
    private readonly IUAuthRequestClient _post;
    private readonly UAuthClientOptions _options;
    private readonly UAuthClientDiagnostics _diagnostics;
    private readonly NavigationManager _nav;

    public UAuthFlowClient(IUAuthRequestClient post, IOptions<UAuthClientOptions> options, UAuthClientDiagnostics diagnostics, NavigationManager nav)
    {
        _post = post;
        _options = options.Value;
        _diagnostics = diagnostics;
        _nav = nav;
    }

    private string Url(string path) => UAuthUrlBuilder.Build(_options.Endpoints.BasePath, path, _options.MultiTenant);

    public async Task LoginAsync(LoginRequest request)
    {
        var canPost = ClientLoginCapabilities.CanPostCredentials(_options.ClientProfile);

        if (!_options.Login.AllowCredentialPost && !canPost)
        {
            throw new InvalidOperationException("Direct credential posting is disabled for this client profile. " +
                "Public clients (e.g. Blazor WASM) MUST use PKCE-based login flows. " +
                "If this is a trusted server-hosted client, you may explicitly enable " +
                "Login.AllowCredentialPost, but doing so is insecure for public clients.");
        }

        var url = Url(_options.Endpoints.Login);
        await _post.NavigateAsync(url, request.ToDictionary());
    }

    public async Task LoginAsync(LoginRequest request, string? returnUrl)
    {
        var canPost = ClientLoginCapabilities.CanPostCredentials(_options.ClientProfile);

        if (!_options.Login.AllowCredentialPost && !canPost)
            throw new InvalidOperationException("Direct credential posting is disabled for this client profile. " +
                "Public clients (e.g. Blazor WASM) MUST use PKCE-based login flows. " +
                "If this is a trusted server-hosted client, you may explicitly enable " +
                "Login.AllowCredentialPost, but doing so is insecure for public clients.");

        var resolvedReturnUrl =
            returnUrl
            ?? _options.Login.ReturnUrl
            ?? _options.DefaultReturnUrl;

        var payload = request.ToDictionary();

        if (!string.IsNullOrWhiteSpace(resolvedReturnUrl))
            payload["return_url"] = resolvedReturnUrl;

        var url = Url(_options.Endpoints.Login);
        await _post.NavigateAsync(url, payload);
    }

    public async Task LogoutAsync()
    {
        var url = Url(_options.Endpoints.Logout);
        await _post.NavigateAsync(url);
    }

    public async Task<RefreshResult> RefreshAsync(bool isAuto = false)
    {
        if (isAuto == false)
        {
            _diagnostics.MarkManualRefresh();
        }

        var url = Url(_options.Endpoints.Refresh);
        var result = await _post.SendFormAsync(url);
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
        var url = Url(_options.Endpoints.Reauth);
        await _post.NavigateAsync(_options.Endpoints.Reauth);
    }

    public async Task<AuthValidationResult> ValidateAsync()
    {
        var url = Url(_options.Endpoints.Validate);
        var raw = await _post.SendFormForJsonAsync(url);

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
        var pkce = _options.Pkce;

        if (!pkce.Enabled)
            throw new InvalidOperationException("PKCE login is disabled by configuration.");

        var verifier = CreateVerifier();
        var challenge = CreateChallenge(verifier);

        var authorizeUrl = Url(_options.Endpoints.PkceAuthorize);

        var raw = await _post.SendFormForJsonAsync(
            authorizeUrl,
            new Dictionary<string, string>
            {
                ["code_challenge"] = challenge,
                ["challenge_method"] = "S256"
            });

        if (!raw.Ok || raw.Body is null)
            throw new InvalidOperationException("PKCE authorize failed.");

        var response = raw.Body.Value.Deserialize<PkceAuthorizeResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (response is null || string.IsNullOrWhiteSpace(response.AuthorizationCode))
            throw new InvalidOperationException("Invalid PKCE authorize response.");

        if (pkce.OnAuthorized is not null)
            await pkce.OnAuthorized(response);

        var resolvedReturnUrl = returnUrl
            ?? pkce.ReturnUrl
            ?? _options.Login.ReturnUrl
            ?? _options.DefaultReturnUrl
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

        if (!_options.Pkce.Enabled)
        {
            throw new InvalidOperationException("PKCE login is disabled by configuration, but a PKCE completion was attempted. " +
                "This usually indicates a misconfiguration or an unexpected redirect flow.");
        }

        var url = Url(_options.Endpoints.PkceComplete);

        var payload = new Dictionary<string, string>
        {
            ["authorization_code"] = request.AuthorizationCode,
            ["code_verifier"] = request.CodeVerifier,
            ["return_url"] = request.ReturnUrl,

            ["Identifier"] = request.Identifier ?? string.Empty,
            ["Secret"] = request.Secret ?? string.Empty
        };

        await _post.NavigateAsync(url, payload);
    }

    private Task NavigateToHubLoginAsync(string authorizationCode, string codeVerifier, string returnUrl)
    {
        var hubLoginUrl = Url(_options.Endpoints.HubLoginPath);

        var data = new Dictionary<string, string>
        {
            ["authorization_code"] = authorizationCode,
            ["code_verifier"] = codeVerifier,
            ["return_url"] = returnUrl,
            ["client_profile"] = _options.ClientProfile.ToString()
        };

        return _post.NavigateAsync(hubLoginUrl, data);
    }


    // ---------------- PKCE CRYPTO ----------------

    private static string CreateVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64Url.Encode(bytes);
    }

    private static string CreateChallenge(string verifier)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
        return Base64Url.Encode(hash);
    }
}
