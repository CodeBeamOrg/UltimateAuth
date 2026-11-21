using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
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
using System.Globalization;
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
    private readonly IUAuthTokenStore _tokens;
    private readonly SemaphoreSlim _credentialGate = new(1);
    private readonly object _refreshLock = new();
    private Task<RefreshResult>? _refreshTask;
    private DateTimeOffset _refreshNotBefore;
    private RefreshResult? _refreshThrottle;

    public UAuthFlowClient(IUAuthRequestClient post, IUAuthClientEvents events, IClientDeviceProvider clientDeviceProvider, IReturnUrlProvider returnUrlProvider, IOptions<UAuthClientOptions> options, UAuthClientDiagnostics diagnostics, IUAuthTokenStore? tokens = null)
    {
        _post = post;
        _events = events;
        _clientDeviceProvider = clientDeviceProvider;
        _returnUrlProvider = returnUrlProvider;
        _options = options.Value;
        _diagnostics = diagnostics;
        _tokens = tokens ?? new InMemoryUAuthTokenStore();
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
                    await _post.NavigateAsync(commitUrl, payload);
                    return new TryLoginResult { IsSuccess = true };
                }

            case UAuthSubmitMode.TryAndCommit:
            default:
                {
                    TryLoginResult result;
                    try { result = await _post.TryAndCommitAsync<TryLoginResult>(tryUrl, commitUrl, payload); }
                    catch (UAuthTransportException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
                    { return new TryLoginResult { Status = 429, ErrorCode = "rate_limited", RetryAfter = ex.RetryAfter }; }

                    if (result is null)
                        throw new UAuthProtocolException("Invalid try-login result.");

                    return result;
                }
        }
    }

    public async Task LogoutAsync(bool navigate = true)
    {
        if (!_options.UsePureJwtBrowserTokens)
        {
            await LogoutCoreAsync(navigate);
            return;
        }

        await _credentialGate.WaitAsync();
        try
        {
            // PureJwt chain revocation requires the authenticated subject and its refresh cookie.
            if (_tokens.Read() is not { } token || token.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                var refresh = await RefreshCoreAsync(applyState: false);
                if (!refresh.IsSuccess && refresh.Outcome != RefreshOutcome.ReauthRequired)
                    throw new UAuthTransportException("Could not obtain a credential for logout.", (HttpStatusCode)refresh.Status) { RetryAfter = refresh.RetryAfter };
            }

            await LogoutCoreAsync(navigate);
        }
        finally
        {
            _credentialGate.Release();
        }
    }

    private async Task LogoutCoreAsync(bool navigate)
    {
        var url = Url(_options.Endpoints.Logout);
        var raw = await _post.SendFormAsync(url, null, navigateOnSuccess: navigate);

        EnsureValidHttpStatus(raw);

        if (raw.Status is < 200 or >= 300)
            throw new UAuthTransportException("Server logout did not succeed.", (HttpStatusCode)raw.Status);

        if (_options.UsePureJwtBrowserTokens)
            await ClearAuthenticationAsync();
        else
            await _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.LogoutVariant, _options.StateEvents.HandlingMode));
    }

    public Task<RefreshResult> RefreshAsync(bool isAuto = false)
        => RefreshAsync(isAuto, CancellationToken.None);

    public Task<RefreshResult> RefreshAsync(bool isAuto, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!isAuto)
            _diagnostics.MarkManualRefresh();

        lock (_refreshLock)
        {
            if (_refreshTask is null || _refreshTask.IsCompleted)
                _refreshTask = RefreshCoordinatedAsync();

            // A canceled waiter does not interrupt a request that may have rotated the cookie.
            return _refreshTask.WaitAsync(ct);
        }
    }

    private async Task<RefreshResult> RefreshCoordinatedAsync()
    {
        await _credentialGate.WaitAsync();
        try
        {
            return await RefreshCoreAsync();
        }
        finally
        {
            _credentialGate.Release();
        }
    }

    private async Task<RefreshResult> RefreshCoreAsync(bool applyState = true)
    {
        var now = DateTimeOffset.UtcNow;
        if (_refreshThrottle is not null && now < _refreshNotBefore)
            return _refreshThrottle with { RetryAfter = _refreshNotBefore - now };
        var raw = await _post.SendFormAsync(Url(_options.Endpoints.Refresh));
        if (raw.Status == 429)
        {
            _refreshThrottle = new RefreshResult
            {
                Status = 429,
                IsSuccess = false,
                Outcome = RefreshOutcome.None,
                ErrorCode = "rate_limited",
                RetryAfter = UAuthRateLimit.ParseRetryAfter(raw.RetryAfter, DateTimeOffset.UtcNow)
            };
            _refreshNotBefore = UAuthRateLimit.NotBefore(DateTimeOffset.UtcNow, _refreshThrottle.RetryAfter);
            return _refreshThrottle;
        }
        _refreshThrottle = null;

        // 401 is a normal authentication outcome for refresh.
        if (raw.Status == 401)
        {
            _diagnostics.MarkRefreshReauthRequired();
            if (_options.UsePureJwtBrowserTokens)
                await ClearAuthenticationAsync();

            return new RefreshResult
            {
                IsSuccess = false,
                Status = 401,
                Outcome = RefreshOutcome.ReauthRequired
            };
        }

        EnsureValidHttpStatus(raw);

        var outcome = RefreshOutcomeParser.Parse(raw.RefreshOutcome);
        var isSuccess = raw.Status is >= 200 and < 300 && raw.Ok;

        if (_options.UsePureJwtBrowserTokens && isSuccess)
        {
            if (raw.AccessToken is not { Length: > 7 } header ||
                !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
                !DateTimeOffset.TryParseExact(raw.AccessTokenExpiresAt, "O", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var expiresAt) || expiresAt <= DateTimeOffset.UtcNow)
            {
                await ClearAuthenticationAsync();
                throw new UAuthProtocolException("Refresh did not return a usable PureJwt access credential. Check grant delivery and CORS exposure.");
            }

            outcome = RefreshOutcome.Rotated;
            _tokens.Replace(new UAuthAccessToken(header["Bearer ".Length..], expiresAt));
            if (applyState)
            {
                AuthValidationResult validation;
                try
                {
                    validation = await ValidateCoreAsync();
                }
                catch
                {
                    await ClearAuthenticationAsync();
                    throw;
                }

                if (validation.IsValid && validation.Snapshot is { } snapshot)
                {
                    await _events.PublishAsync(new UAuthStateEventArgs<AuthStateSnapshot>(
                        UAuthStateEvent.ValidationCalled, UAuthStateEventHandlingMode.Patch, snapshot));
                }
                else
                {
                    await ClearAuthenticationAsync();
                    _diagnostics.MarkRefreshReauthRequired();
                    return new RefreshResult { Status = 401, Outcome = RefreshOutcome.ReauthRequired };
                }
            }
        }

        // Never record a successful refresh outcome
        // for a failed HTTP response.
        if (isSuccess)
        {
            switch (outcome)
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
        }
        else if (outcome == RefreshOutcome.ReauthRequired)
        {
            _diagnostics.MarkRefreshReauthRequired();
        }

        return new RefreshResult
        {
            IsSuccess = isSuccess,
            Status = raw.Status,
            Outcome = outcome
        };
    }

    //public async Task ReauthAsync()
    //{
    //    var url = Url(_options.Endpoints.Reauth);
    //    await _post.NavigateAsync(url);
    //}

    private Task ClearAuthenticationAsync()
    {
        _tokens.Clear();
        return _events.PublishAsync(new UAuthStateEventArgsEmpty(UAuthStateEvent.LogoutVariant, UAuthStateEventHandlingMode.Patch));
    }

    public async Task<AuthValidationResult> ValidateAsync()
    {
        if (!_options.UsePureJwtBrowserTokens)
            return await ValidateCoreAsync();

        await _credentialGate.WaitAsync();
        try
        {
            var result = await ValidateCoreAsync();
            if (result.IsValid && result.Snapshot is { } snapshot)
                await _events.PublishAsync(new UAuthStateEventArgs<AuthStateSnapshot>(
                    UAuthStateEvent.ValidationCalled, UAuthStateEventHandlingMode.Patch, snapshot));
            else
                await ClearAuthenticationAsync();

            return result;
        }
        finally
        {
            _credentialGate.Release();
        }
    }

    private async Task<AuthValidationResult> ValidateCoreAsync()
    {
        var url = Url(_options.Endpoints.Validate);
        var raw = await _post.SendFormAsync(url);

        if (raw.Status == 429) throw UAuthRateLimit.Exception(raw.RetryAfter);
        if (raw.Status == 0)
            throw new UAuthTransportException("Network error during validation.");

        if (raw.Status >= 500)
            throw new UAuthTransportException("Server error during validation.", (HttpStatusCode)raw.Status);

        if (_options.UsePureJwtBrowserTokens && raw.Status == 401)
            return new AuthValidationResult { State = SessionState.Invalid };

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
            if (!_options.UsePureJwtBrowserTokens)
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

    public async Task<TryPkceLoginResult> TryCompletePkceLoginAsync(PkceCompleteRequest request, UAuthSubmitMode mode)
    {
        if (mode == UAuthSubmitMode.DirectCommit)
        {
            await CompletePkceLoginAsync(request);
            return new TryPkceLoginResult { IsSuccess = true };
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
            ["Secret"] = request.Secret ?? string.Empty,
            ["hub_session_id"] = request.HubSessionId ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(request.ReturnUrl))
        {
            payload[UAuthConstants.Form.ReturnUrl] = request.ReturnUrl;
        }

        switch (mode)
        {
            case UAuthSubmitMode.TryOnly:
                {
                    var raw = await _post.SendJsonAsync(tryUrl, request);
                    if (raw.Status == 429) return new TryPkceLoginResult { Status = 429, ErrorCode = "rate_limited", RetryAfter = UAuthRateLimit.ParseRetryAfter(raw.RetryAfter, DateTimeOffset.UtcNow) };

                    EnsureValidHttpStatus(raw);

                    if (raw.Status is >= 400 and < 500)
                    {
                        throw new UAuthProtocolException($"Unexpected client error during PKCE try-login: {raw.Status}.");
                    }

                    if (raw.Body is null || raw.Body.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                    {
                        throw new UAuthProtocolException("Empty PKCE try-login response body.");
                    }

                    try
                    {
                        var parsed = raw.Body.Value.Deserialize<TryPkceLoginResult>(
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        return parsed ?? throw new UAuthProtocolException("Invalid PKCE try-login result.");
                    }
                    catch (JsonException ex)
                    {
                        throw new UAuthProtocolException("Invalid PKCE try-login result.", ex);
                    }
                }

            case UAuthSubmitMode.TryAndCommit:
            default:
                {
                    TryPkceLoginResult result;
                    try { result = await _post.TryAndCommitAsync<TryPkceLoginResult>(tryUrl, commitUrl, payload); }
                    catch (UAuthTransportException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
                    { return new TryPkceLoginResult { Status = 429, ErrorCode = "rate_limited", RetryAfter = ex.RetryAfter }; }

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
            [UAuthConstants.Form.ReturnUrl] = request.ReturnUrl ?? string.Empty,

            ["Identifier"] = request.Identifier ?? string.Empty,
            ["Secret"] = request.Secret ?? string.Empty,

            ["hub_session_id"] = request.HubSessionId ?? string.Empty,
        };

        await _post.NavigateAsync(url, payload);
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
