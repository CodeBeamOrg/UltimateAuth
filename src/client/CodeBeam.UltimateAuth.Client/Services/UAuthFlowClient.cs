using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
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
    private readonly IUAuthTokenStore _tokenStore;
    private readonly IUAuthRefreshCoordinator _refreshCoordinator;

    public UAuthFlowClient(
        IUAuthRequestClient post,
        IUAuthClientEvents events,
        IClientDeviceProvider clientDeviceProvider,
        IReturnUrlProvider returnUrlProvider,
        IOptions<UAuthClientOptions> options,
        IUAuthTokenStore tokenStore,
        IUAuthRefreshCoordinator refreshCoordinator)
    {
        _post = post;
        _events = events;
        _clientDeviceProvider = clientDeviceProvider;
        _returnUrlProvider = returnUrlProvider;
        _options = options.Value;
        _tokenStore = tokenStore;
        _refreshCoordinator = refreshCoordinator;
    }

    private string Url(string path) => UAuthUrlBuilder.Build(_options.Endpoints.BasePath, path, _options.MultiTenant);

    public async Task<UAuthResult> LoginAsync(LoginRequest request, string? returnUrl = null, UAuthLoginTransport transport = UAuthLoginTransport.Auto)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureCanPost();

        var resolvedReturnUrl =
            returnUrl
            ?? _options.Login.ReturnUrl
            ?? _options.DefaultReturnUrl;

        var payload = BuildPayload(request, resolvedReturnUrl);

        var info = await _post.GetLoginTransportAsync(Url(_options.Endpoints.LoginTransport));

        if (!info.Supported)
        {
            throw new NotSupportedException("The effective credential delivery is not supported by this UltimateAuth client.");
        }

        var selected = transport switch
        {
            UAuthLoginTransport.Auto =>
                info.NavigationSupported && !string.IsNullOrWhiteSpace(resolvedReturnUrl)
                    ? UAuthLoginTransport.Navigation
                    : UAuthLoginTransport.Fetch,

            UAuthLoginTransport.Fetch =>
                UAuthLoginTransport.Fetch,

            UAuthLoginTransport.Navigation =>
                UAuthLoginTransport.Navigation,

            _ => throw new ArgumentOutOfRangeException(
                nameof(transport))
        };

        if (selected == UAuthLoginTransport.Navigation)
        {
            if (!info.NavigationSupported)
            {
                throw new InvalidOperationException("Navigation transport is incompatible with header-based credential delivery.");
            }

            await _post.NavigateAsync(Url(_options.Endpoints.Login), payload);

            // The browser now owns the native POST.
            // HTTP authentication success cannot be observed here.
            return new UAuthResult
            {
                IsSuccess = true,
                Status = 202
            };
        }

        payload["__uauth_response_mode"] = "fetch";

        var raw = await _post.SendFormAsync(Url(_options.Endpoints.Login), payload);

        EnsureValidHttpStatus(raw);

        if (!raw.Ok)
        {
            return new UAuthResult
            {
                IsSuccess = false,
                Status = raw.Status
            };
        }

        await UAuthLoginCompletion.CompleteAsync(raw, info.AccessTokenInHeader, _tokenStore, _events);

        if (!string.IsNullOrWhiteSpace(resolvedReturnUrl))
        {
            var redirectUrl = UAuthLoginCompletion.GetRedirectUrl(raw);

            if (!string.IsNullOrWhiteSpace(redirectUrl))
            {
                await _post.NavigateToUrlAsync(redirectUrl);
            }
        }

        return new UAuthResult
        {
            IsSuccess = true,
            Status = raw.Status
        };
    }

    public async Task<TryLoginResult> TryLoginAsync(LoginRequest request, UAuthSubmitMode mode, string? returnUrl = null, UAuthLoginTransport transport = UAuthLoginTransport.Auto)
    {
        EnsureCanPost();

        var payload = BuildPayload(request, returnUrl);

        var tryUrl = Url(_options.Endpoints.TryLogin);
        var commitUrl = Url(_options.Endpoints.Login);

        switch (mode)
        {
            case UAuthSubmitMode.TryOnly:
                {
                    var raw = await _post.SendJsonAsync(tryUrl, payload);

                    if (raw.Status == 429) return new TryLoginResult { Status = 429, ErrorCode = "rate_limited", RetryAfter = UAuthRateLimit.ParseRetryAfter(raw.RetryAfter, DateTimeOffset.UtcNow) };
                    EnsureValidHttpStatus(raw);

                    if (raw.Status is >= 400 and < 500)
                    {
                        throw new UAuthProtocolException($"Unexpected client error during try-login: {raw.Status}.");
                    }

                    if (raw.Body is null || raw.Body.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                    {
                        throw new UAuthProtocolException("Empty try-login response body.");
                    }

                    try
                    {
                        var parsed = raw.Body.Value.Deserialize<TryLoginResult>(
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        return parsed ?? throw new UAuthProtocolException("Invalid try-login result.");
                    }
                    catch (JsonException ex)
                    {
                        throw new UAuthProtocolException("Invalid try-login result.", ex);
                    }
                }

            case UAuthSubmitMode.DirectCommit:
                {
                    var commit = await LoginAsync(
                        request,
                        returnUrl,
                        transport);

                    return new TryLoginResult
                    {
                        IsSuccess = commit.IsSuccess,
                        Status = commit.Status
                    };
                }

            case UAuthSubmitMode.TryAndCommit:
            default:
                {
                    var preview = await TryLoginAsync(
                        request,
                        UAuthSubmitMode.TryOnly,
                        returnUrl,
                        transport);

                    if (!preview.IsSuccess)
                        return preview;

                    var commitRequest = request with
                    {
                        PreviewReceipt = preview.PreviewReceipt
                    };

                    var commit = await LoginAsync(
                        commitRequest,
                        returnUrl,
                        transport);

                    if (!commit.IsSuccess)
                    {
                        return new TryLoginResult
                        {
                            IsSuccess = false,
                            Status = commit.Status,
                            ErrorCode = "commit_failed"
                        };
                    }

                    return preview;
                }
        }
    }

    public async Task LogoutAsync(bool navigate = true)
    {
        var url = Url(_options.Endpoints.Logout);
        var raw = await _post.SendFormAsync(url, null, navigateOnSuccess: navigate);

        EnsureValidHttpStatus(raw);

        if (raw.Status is < 200 or >= 300)
            throw new UAuthTransportException("Server logout did not succeed.", (HttpStatusCode)raw.Status);

        _tokenStore.Clear();
        await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.LogoutVariant, _options.StateEvents.HandlingMode));
    }

    public Task<RefreshResult> RefreshAsync(bool isAuto = false)
    {
        return _refreshCoordinator.RefreshAsync(isAuto
            ? UAuthRefreshTrigger.Background
            : UAuthRefreshTrigger.Manual);
    }

    public Task<RefreshResult> RecoverAsync()
    {
        return _refreshCoordinator.RefreshAsync(UAuthRefreshTrigger.Recovery);
    }


    //public async Task ReauthAsync()
    //{
    //    var url = Url(_options.Endpoints.Reauth);
    //    await _post.NavigateAsync(url);
    //}

    public async Task<AuthValidationResult> ValidateAsync()
    {
        var url = Url(_options.Endpoints.Validate);

        var accessToken = _tokenStore.Read();

        if (accessToken is not null && accessToken.IsExpired(DateTimeOffset.UtcNow))
        {
            accessToken = null;
        }

        var raw = await _post.SendFormAsync(url, form: null, accessToken: accessToken?.Token);

        if (raw.Status == 429) throw UAuthRateLimit.Exception(raw.RetryAfter);
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

        EnsureValidHttpStatus(raw);

        if (raw.Status is >= 400 and < 500)
        {
            throw new UAuthProtocolException($"PKCE authorize request failed: {raw.Status}.");
        }

        if (raw.Body is null || raw.Body.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            throw new UAuthProtocolException("Missing PKCE authorize response body.");
        }

        PkceAuthorizeResponse response;

        try
        {
            response = raw.Body.Value.Deserialize<PkceAuthorizeResponse>(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new UAuthProtocolException("Invalid PKCE authorize response.");
        }
        catch (JsonException ex)
        {
            throw new UAuthProtocolException("Invalid PKCE authorize response format.", ex);
        }

        if (string.IsNullOrWhiteSpace(response.AuthorizationCode))
            throw new UAuthProtocolException("PKCE authorize response has no authorization code.");

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

    public async Task<TryPkceLoginResult> TryCompletePkceLoginAsync(
        PkceCompleteRequest request,
        UAuthSubmitMode mode)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_options.Pkce.Enabled)
            throw new InvalidOperationException("PKCE login is disabled.");

        if (mode == UAuthSubmitMode.DirectCommit)
        {
            await CompletePkceLoginAsync(request);

            return new TryPkceLoginResult
            {
                IsSuccess = true
            };
        }

        var raw = await _post.SendJsonAsync(
            Url(_options.Endpoints.PkceTryComplete),
            request);

        if (raw.Status == 429)
        {
            return new TryPkceLoginResult
            {
                Status = 429,
                RetryAfter = UAuthRateLimit.ParseRetryAfter(
                    raw.RetryAfter,
                    DateTimeOffset.UtcNow),
                ErrorCode = "rate_limited"
            };
        }

        EnsureValidHttpStatus(raw);

        if (raw.Body is null ||
            raw.Body.Value.ValueKind is
                JsonValueKind.Null or JsonValueKind.Undefined)
        {
            throw new UAuthProtocolException(
                "Empty PKCE try-login response body.");
        }

        TryPkceLoginResult preview;

        try
        {
            preview = raw.Body.Value.Deserialize<TryPkceLoginResult>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new UAuthProtocolException(
                    "Invalid PKCE try-login result.");
        }
        catch (JsonException ex)
        {
            throw new UAuthProtocolException(
                "Invalid PKCE try-login result.", ex);
        }

        if (mode == UAuthSubmitMode.TryOnly || !preview.IsSuccess)
            return preview;

        await CompletePkceLoginAsync(request);

        return preview;
    }

    public async Task CompletePkceLoginAsync(
        PkceCompleteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_options.Pkce.Enabled)
            throw new InvalidOperationException(
                "PKCE login is disabled.");

        var url = Url(_options.Endpoints.PkceComplete);

        var payload = new Dictionary<string, string>
        {
            ["authorization_code"] = request.AuthorizationCode,
            ["code_verifier"] = request.CodeVerifier,
            [UAuthConstants.Form.ReturnUrl] =
                request.ReturnUrl ?? string.Empty,
            ["Identifier"] = request.Identifier ?? string.Empty,
            ["Secret"] = request.Secret ?? string.Empty,
            ["hub_session_id"] = request.HubSessionId ?? string.Empty
        };

        var info = await _post.GetLoginTransportAsync(
            Url(_options.Endpoints.LoginTransport));

        if (!info.Supported)
            throw new NotSupportedException(
                "PKCE credential delivery is not supported.");

        if (info.NavigationSupported)
        {
            await _post.NavigateAsync(url, payload);
            return;
        }

        payload["__uauth_response_mode"] = "fetch";

        var raw = await _post.SendFormAsync(url, payload);

        EnsureValidHttpStatus(raw);

        if (!raw.Ok)
            throw new UAuthProtocolException($"PKCE completion failed: HTTP {raw.Status}.");

        var redirectUrl = UAuthLoginCompletion.GetRedirectUrl(raw);

        if (!string.IsNullOrWhiteSpace(request.HubSessionId))
        {
            if (string.IsNullOrWhiteSpace(redirectUrl) ||
                string.IsNullOrWhiteSpace(request.ReturnUrl))
            {
                throw new UAuthProtocolException("Missing PKCE handoff redirect.");
            }

            // Do not authenticate the Hub using the initiating application's access token.
            await _post.NavigateToPkceReturnUrlAsync(redirectUrl, request.ReturnUrl);

            return;
        }

        await UAuthLoginCompletion.CompleteAsync(raw, info.AccessTokenInHeader, _tokenStore, _events);

        if (!string.IsNullOrWhiteSpace(redirectUrl))
        {
            await _post.NavigateToUrlAsync(redirectUrl);
        }
    }

    public async Task<UAuthResult<RevokeResult>> LogoutMyDeviceAsync(LogoutDeviceRequest request)
    {
        var raw = await _post.SendJsonAsync(Url($"/me/logout-device"), request);

        if (raw.Ok)
        {
            var result = UAuthResultMapper.FromJson<RevokeResult>(raw);

            if (result.IsSuccess && result.Value?.CurrentChain == true)
            {
                await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.LogoutVariant, _options.StateEvents.HandlingMode));
            }

            return result;
        }

        return UAuthResultMapper.FromJson<RevokeResult>(raw);
    }

    public async Task<UAuthResult<RevokeResult>> LogoutUserDeviceAsync(UserKey userKey, LogoutDeviceRequest request)
    {
        var raw = await _post.SendJsonAsync(Url($"/admin/users/{userKey.Value}/logout-device"), request);
        return UAuthResultMapper.FromJson<RevokeResult>(raw);
    }

    public async Task<UAuthResult> LogoutMyOtherDevicesAsync()
    {
        var raw = await _post.SendJsonAsync(Url("/me/logout-others"));
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> LogoutUserOtherDevicesAsync(UserKey userKey, LogoutOtherDevicesRequest request)
    {
        var raw = await _post.SendJsonAsync(Url($"/admin/users/{userKey.Value}/logout-others"), request);
        return UAuthResultMapper.From(raw);
    }

    public async Task<UAuthResult> LogoutAllMyDevicesAsync()
    {
        var raw = await _post.SendJsonAsync(Url("/me/logout-all"));
        var result = UAuthResultMapper.From(raw);

        if (result.IsSuccess)
        {
            await _events.PublishAsync(
                new UAuthStateEventArgsEmpty(UAuthStateEvent.LogoutVariant, _options.StateEvents.HandlingMode));
        }

        return result;
    }

    public async Task<UAuthResult> LogoutAllUserDevicesAsync(UserKey userKey)
    {
        var raw = await _post.SendJsonAsync(Url($"/admin/users/{userKey.Value}/logout-all"));
        return UAuthResultMapper.From(raw);
    }

    private void EnsureCanPost()
    {
        var canPost = ClientLoginCapabilities.CanPostCredentials(_options.ApplicationProfile);

        if (!_options.Login.AllowCredentialPost && !canPost)
        {
            throw new InvalidOperationException("Direct credential posting is disabled for this application profile. " +
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
            payload[UAuthConstants.Form.ReturnUrl] = resolvedReturnUrl;
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
            [UAuthConstants.Form.ReturnUrl] = returnUrl,
            ["application_profile"] = _options.ApplicationProfile.ToString(),
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

    private static void EnsureValidHttpStatus(UAuthTransportResult raw)
    {
        if (raw is null)
            throw new UAuthProtocolException("Transport client returned a null response.");

        if (raw.Status == 429) throw UAuthRateLimit.Exception(raw.RetryAfter);
        if (raw.Status == 0)
            throw new UAuthTransportException("Network error.");

        if (raw.Status >= 500 && raw.Status <= 599)
            throw new UAuthTransportException($"Server error {raw.Status}.", (HttpStatusCode)raw.Status);

        if (raw.Status < 200 || raw.Status is >= 300 and < 400 || raw.Status >= 600)
        {
            throw new UAuthProtocolException($"Unexpected HTTP status code: {raw.Status}.");
        }
    }
}
