using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Client.Services;

internal class UAuthFlowClient : IFlowClient
{
    private readonly IUAuthRequestClient _post;
    private readonly IUAuthClientEvents _events;
    private readonly IClientDeviceProvider _clientDeviceProvider;
    private readonly IReturnUrlProvider _returnUrlProvider;
    private readonly UAuthClientOptions _options;
    private readonly UAuthClientDiagnostics _diagnostics;

    public UAuthFlowClient(IUAuthRequestClient post, IUAuthClientEvents events, IClientDeviceProvider clientDeviceProvider, IReturnUrlProvider returnUrlProvider, IOptions<UAuthClientOptions> options, UAuthClientDiagnostics diagnostics)
    {
        _post = post;
        _events = events;
        _clientDeviceProvider = clientDeviceProvider;
        _returnUrlProvider = returnUrlProvider;
        _options = options.Value;
        _diagnostics = diagnostics;
    }

    private string Url(string path) => UAuthUrlBuilder.Build(_options.Endpoints.BasePath, path, _options.MultiTenant);

    public async Task LoginAsync(LoginRequest request, string? returnUrl = null)
    {
        EnsureCanPost();

        var payload = BuildPayload(request, returnUrl);

        var url = Url(_options.Endpoints.Login);
        await _post.NavigateAsync(url, payload);
    }

    public async Task<TryLoginResult> TryLoginAsync(LoginRequest request, UAuthSubmitMode mode, string? returnUrl = null)
    {
        EnsureCanPost();

        var payload = BuildPayload(request, returnUrl);

        var tryUrl = Url(_options.Endpoints.TryLogin);
        var commitUrl = Url(_options.Endpoints.Login);

        switch (mode)
        {
            case UAuthSubmitMode.TryOnly:
                {
                    var result = await _post.SendJsonAsync(tryUrl, payload);

                    if (result.Body is null)
                        throw new UAuthProtocolException("Empty response body.");

                    TryLoginResult parsed;

                    try
                    {
                        parsed = result.Body.Value.Deserialize<TryLoginResult>(
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                    }
                    catch (JsonException ex)
                    {
                        throw new UAuthProtocolException("Invalid try-login result.", ex);
                    }

                    if (parsed is null)
                        throw new UAuthProtocolException("Invalid try-login result.");

                    return parsed;
                }

            case UAuthSubmitMode.DirectCommit:
                {
                    await _post.NavigateAsync(commitUrl, payload);
                    return new TryLoginResult { Success = true };
                }

            case UAuthSubmitMode.TryAndCommit:
            default:
                {
                    var result = await _post.TryAndCommitAsync<TryLoginResult>(tryUrl, commitUrl, payload);

                    if (result is null)
                        throw new UAuthProtocolException("Invalid try-login result.");

                    return result;
                }
        }
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

        if (result.Status == 401)
        {
            _diagnostics.MarkRefreshReauthRequired();
            return new RefreshResult
            {
                IsSuccess = false,
                Status = result.Status,
                Outcome = RefreshOutcome.ReauthRequired
            };
        }

        var refreshOutcome = RefreshOutcomeParser.Parse(result.RefreshOutcome);
        switch (refreshOutcome)
        {
            case RefreshOutcome.NoOp:
                _diagnostics.MarkRefreshNoOp();
                break;
            case RefreshOutcome.Touched:
                _diagnostics.MarkRefreshTouched();
                break;
            case RefreshOutcome.Rotated:
                _diagnostics.MarkRefreshRotated();
                break;
            case RefreshOutcome.ReauthRequired:
                _diagnostics.MarkRefreshReauthRequired();
                break;
            case RefreshOutcome.Success:
                _diagnostics.MarkRefreshSuccess();
                break;
        }

        return new RefreshResult
        {
            IsSuccess = result.Ok,
            Status = result.Status,
            Outcome = refreshOutcome
        };
    }

    //public async Task ReauthAsync()
    //{
    //    var url = Url(_options.Endpoints.Reauth);
    //    await _post.NavigateAsync(url);
    //}

    public async Task<AuthValidationResult> ValidateAsync()
    {
        var url = Url(_options.Endpoints.Validate);
        var raw = await _post.SendFormAsync(url);

        if (raw.Status == 0)
            throw new UAuthTransportException("Network error during validation.");

        if (raw.Status >= 500)
            throw new UAuthTransportException("Server error during validation.", (HttpStatusCode)raw.Status);

        if (raw.Body is null)
            throw new UAuthProtocolException("Validation response body was empty.");

        AuthValidationResult? body;

        try
        {
            body = raw.Body.Value.Deserialize<AuthValidationResult>(
                new JsonSerializerOptions{ PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            throw new UAuthProtocolException("Invalid validation response format.", ex);
        }

        if (body is null)
            throw new UAuthProtocolException("Malformed validation response.");

        if (raw.Status == 401 || (raw.Status >= 200 && raw.Status < 300))
        {
            // Don't set refresh mode to validate here, it's already validated.
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.ValidationCalled, UAuthStateEventHandlingMode.Patch));
            return body;
        }

        if (raw.Status >= 400 && raw.Status < 500)
            throw new UAuthProtocolException($"Unexpected client error during validation: {raw.Status}");

        throw new UAuthTransportException($"Unexpected status code: {raw.Status}", (HttpStatusCode)raw.Status);
    }

    public async Task BeginPkceAsync(string? returnUrl = null)
    {
        var pkce = _options.Pkce;
        var device = await _clientDeviceProvider.GetAsync();

        if (!pkce.Enabled)
            throw new InvalidOperationException("PKCE login is disabled by configuration.");

        var verifier = CreateVerifier();
        var challenge = CreateChallenge(verifier);

        var authorizeUrl = Url(_options.Endpoints.PkceAuthorize);

        var raw = await _post.SendFormAsync(
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
            ?? _returnUrlProvider.GetCurrentUrl();

        if (pkce.AutoRedirect)
        {
            await NavigateToHubLoginAsync(response.AuthorizationCode, verifier, resolvedReturnUrl, device);
        }
    }

    public async Task<TryPkceLoginResult> TryCompletePkceLoginAsync(PkceCompleteRequest request, UAuthSubmitMode mode)
    {
        if (mode == UAuthSubmitMode.DirectCommit)
        {
            await CompletePkceLoginAsync(request);
            return new TryPkceLoginResult { Success = true };
        }

        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (!_options.Pkce.Enabled)
            throw new InvalidOperationException("PKCE login is disabled.");

        var tryUrl = Url(_options.Endpoints.PkceTryComplete);
        var commitUrl = Url(_options.Endpoints.PkceComplete);

        var payload = new Dictionary<string, string>
        {
            ["authorization_code"] = request.AuthorizationCode,
            ["code_verifier"] = request.CodeVerifier,
            ["Identifier"] = request.Identifier ?? string.Empty,
            ["Secret"] = request.Secret ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(request.ReturnUrl))
        {
            payload["return_url"] = request.ReturnUrl;
        }

        switch (mode)
        {
            case UAuthSubmitMode.TryOnly:
                {
                    var raw = await _post.SendJsonAsync(tryUrl, request);

                    if (raw.Body is null)
                        throw new UAuthProtocolException("Empty response body.");

                    var parsed = raw.Body.Value.Deserialize<TryPkceLoginResult>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (parsed is null)
                        throw new UAuthProtocolException("Invalid PKCE try result.");

                    return parsed;
                }

            case UAuthSubmitMode.TryAndCommit:
            default:
                {
                    var result = await _post.TryAndCommitAsync<TryPkceLoginResult>(tryUrl, commitUrl, payload);

                    if (result is null)
                        throw new UAuthProtocolException("Invalid PKCE try result.");

                    return result;
                }
        }
    }

    public async Task CompletePkceLoginAsync(PkceCompleteRequest request)
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
            ["Secret"] = request.Secret ?? string.Empty,

            ["hub_session_id"] = request.HubSessionId ?? string.Empty,
        };

        await _post.NavigateAsync(url, payload);
    }

    public async Task<UAuthResult<RevokeResult>> LogoutDeviceSelfAsync(LogoutDeviceRequest request)
    {
        var raw = await _post.SendJsonAsync(Url($"/me/logout-device"), request);

        if (raw.Ok)
        {
            var result = UAuthResultMapper.FromJson<RevokeResult>(raw);

            if (result.Value?.CurrentChain == true)
            {
                await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.LogoutVariant, _options.StateEvents.HandlingMode));
            }

            return result;
        }

        return UAuthResultMapper.FromJson<RevokeResult>(raw);
    }

    public async Task<UAuthResult<RevokeResult>> LogoutDeviceAdminAsync(UserKey userKey, LogoutDeviceRequest request)
    {
        var raw = await _post.SendJsonAsync(Url($"/admin/users/{userKey.Value}/logout-device"), request);
        return UAuthResultMapper.FromJson<RevokeResult>(raw);
    }

    public async Task<UAuthResult> LogoutOtherDevicesSelfAsync()
    {
        var raw = await _post.SendJsonAsync(Url("/me/logout-others"));
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> LogoutOtherDevicesAdminAsync(UserKey userKey, LogoutOtherDevicesAdminRequest request)
    {
        var raw = await _post.SendJsonAsync(Url($"/admin/users/{userKey.Value}/logout-others"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> LogoutAllDevicesSelfAsync()
    {
        var raw = await _post.SendJsonAsync(Url("/me/logout-all"));
        if (raw.Ok)
        {
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.LogoutVariant, _options.StateEvents.HandlingMode));
        }
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> LogoutAllDevicesAdminAsync(UserKey userKey)
    {
        var raw = await _post.SendJsonAsync(Url($"/admin/users/{userKey.Value}/logout-all"));
        return UAuthResultMapper.From(raw);
    }


    private void EnsureCanPost()
    {
        var canPost = ClientLoginCapabilities.CanPostCredentials(_options.ClientProfile);

        if (!_options.Login.AllowCredentialPost && !canPost)
        {
            throw new InvalidOperationException("Direct credential posting is disabled for this client profile. " +
                "Public clients (e.g. Blazor WASM) MUST use PKCE-based login flows. " +
                "If this is a trusted server-hosted client, you may explicitly enable " +
                "Login.AllowCredentialPost, but doing so is insecure for public clients.");
        }
    }

    private IDictionary<string, string> BuildPayload(LoginRequest request, string? returnUrl)
    {
        var payload = request.ToDictionary();

        var resolvedReturnUrl =
            returnUrl
            ?? _options.Login.ReturnUrl
            ?? _options.DefaultReturnUrl;

        if (!string.IsNullOrWhiteSpace(resolvedReturnUrl))
        {
            payload["return_url"] = resolvedReturnUrl;
        }

        return payload;
    }

    private Task NavigateToHubLoginAsync(string authorizationCode, string codeVerifier, string returnUrl, DeviceContext device)
    {
        var hubLoginUrl = Url(_options.Endpoints.HubLoginPath);

        var deviceJson = JsonSerializer.Serialize(device);
        var deviceEncoded = Base64Url.Encode(Encoding.UTF8.GetBytes(deviceJson));

        var data = new Dictionary<string, string>
        {
            ["authorization_code"] = authorizationCode,
            ["code_verifier"] = codeVerifier,
            ["return_url"] = returnUrl,
            ["client_profile"] = _options.ClientProfile.ToString(),
            ["device"] = deviceEncoded
        };

        return _post.NavigateAsync(hubLoginUrl, data);
    }

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
