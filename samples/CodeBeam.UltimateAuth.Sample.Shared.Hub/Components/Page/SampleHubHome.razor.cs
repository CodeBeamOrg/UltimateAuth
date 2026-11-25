using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Runtime;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Stores;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Hub.Components.Page;

public partial class SampleHubHome
{
    private UAuthClientProductInfo? _productInfo;
    private UAuthLoginForm _loginForm = null!;

    private CancellationTokenSource? _lockoutCts;
    private PeriodicTimer? _lockoutTimer;
    private DateTimeOffset? _lockoutUntil;
    private TimeSpan _remaining;
    private bool _isLocked;
    private DateTimeOffset? _lockoutStartedAt;
    private TimeSpan _lockoutDuration;
    private double _progressPercent;
    private int? _remainingAttempts = null;
    private bool _errorHandled;

    protected override async Task OnInitializedAsync()
    {
        _productInfo = ClientProductInfoProvider.Get();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (string.IsNullOrWhiteSpace(HubKey))
            return;

        if (HubState is null || !HubState.Exists)
        {
            return;
        }

        if (HubState.IsExpired)
        {
            await ContinuePkceAsync();
            return;
        }

        if (HubState.Error != null && !_errorHandled)
        {
            _errorHandled = true;
            Snackbar.Add(ResolveErrorMessage(HubState.Error), Severity.Error);
            await ContinuePkceAsync();

            if (HubSessionId.TryParse(HubKey, out var hubSessionId))
            {
                await ReloadStateAsync();
            }

            await _loginForm.ReloadAsync();

            StateHasChanged();
        }
    }

    // For testing & debugging
    private async Task ProgrammaticPkceLogin()
    {
        var hub = HubState;

        if (hub is null)
            return;

        if (!HubSessionId.TryParse(HubKey, out var hubSessionId))
            return;

        var credentials = await HubCredentialResolver.ResolveAsync(hubSessionId);

        var request = new PkceCompleteRequest
        {
            Identifier = "admin",
            Secret = "admin",
            AuthorizationCode = credentials?.AuthorizationCode ?? string.Empty,
            CodeVerifier = credentials?.CodeVerifier ?? string.Empty,
            ReturnUrl = HubState?.ReturnUrl ?? string.Empty,
            HubSessionId = HubState?.HubSessionId.Value ?? hubSessionId.Value,
        };

        await UAuthClient.Flows.TryCompletePkceLoginAsync(request, UAuthSubmitMode.TryAndCommit);
    }

    private async Task HandleLoginResult(IUAuthTryResult result)
    {
        if (result is TryPkceLoginResult pkce)
        {
            if (!result.IsSuccess)
            {
                if (result.Reason == AuthFailureReason.LockedOut && result.LockoutUntilUtc is { } until)
                {
                    _lockoutUntil = until;
                    StartCountdown();
                }

                _remainingAttempts = result.RemainingAttempts;

                ShowLoginError(result.Reason, result.RemainingAttempts);
                await ContinuePkceAsync();
            }
        }
    }

    private HubCredentials? _pkce;

    private async Task ContinuePkceAsync()
    {
        if (string.IsNullOrWhiteSpace(HubKey))
            return;

        var key = new AuthArtifactKey(HubKey);
        var artifact = await AuthStore.GetAsync(key) as HubFlowArtifact;

        if (artifact is null)
            return;

        _pkce = await PkceService.RefreshAsync(artifact);
        await HubFlowService.ContinuePkceAsync(HubKey, _pkce.AuthorizationCode, _pkce.CodeVerifier);
    }

    private async Task StartNewPkceAsync()
    {
        var returnUrl = await ResolveReturnUrlAsync();
        await UAuthClient.Flows.BeginPkceAsync(returnUrl);
    }

    private async Task<string> ResolveReturnUrlAsync()
    {
        var fromContext = HubState?.ReturnUrl;
        if (!string.IsNullOrWhiteSpace(fromContext))
            return fromContext;

        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue(UAuthConstants.Query.ReturnUrl, out var ru) && !string.IsNullOrWhiteSpace(ru))
            return ru!;

        if (query.TryGetValue(UAuthConstants.Query.Hub, out var hubKey) && !string.IsNullOrWhiteSpace(hubKey))
        {
            var artifact = await AuthStore.GetAsync(new AuthArtifactKey(hubKey!));
            if (artifact is HubFlowArtifact flow && !string.IsNullOrWhiteSpace(flow.ReturnUrl))
                return flow.ReturnUrl!;
        }

        return Navigation.Uri;
    }

    private async void StartCountdown()
    {
        if (_lockoutUntil is null)
            return;

        _isLocked = true;
        _lockoutStartedAt = DateTimeOffset.UtcNow;
        _lockoutDuration = _lockoutUntil.Value - DateTimeOffset.UtcNow;
        UpdateRemaining();

        _lockoutCts?.Cancel();
        _lockoutCts = new CancellationTokenSource();

        _lockoutTimer?.Dispose();
        _lockoutTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (await _lockoutTimer.WaitForNextTickAsync(_lockoutCts.Token))
            {
                UpdateRemaining();

                if (_remaining <= TimeSpan.Zero)
                {
                    ResetLockoutState();
                    await InvokeAsync(StateHasChanged);
                    break;
                }

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {

        }
    }

    private void ResetLockoutState()
    {
        _isLocked = false;
        _lockoutUntil = null;
        _progressPercent = 0;
        _remainingAttempts = null;
    }

    private void UpdateRemaining()
    {
        if (_lockoutUntil is null || _lockoutStartedAt is null)
            return;

        var now = DateTimeOffset.UtcNow;

        _remaining = _lockoutUntil.Value - now;

        if (_remaining <= TimeSpan.Zero)
        {
            _remaining = TimeSpan.Zero;
            return;
        }

        var elapsed = now - _lockoutStartedAt.Value;

        if (_lockoutDuration.TotalSeconds > 0)
        {
            var percent = 100 - (elapsed.TotalSeconds / _lockoutDuration.TotalSeconds * 100);
            _progressPercent = Math.Max(0, percent);
        }
    }

    private void ShowLoginError(AuthFailureReason? reason, int? remainingAttempts)
    {
        string message = reason switch
        {
            AuthFailureReason.InvalidCredentials when remainingAttempts is > 0
                => $"Invalid username or password. {remainingAttempts} attempt(s) remaining.",

            AuthFailureReason.InvalidCredentials
                => "Invalid username or password.",

            AuthFailureReason.RequiresMfa
                => "Multi-factor authentication required.",

            AuthFailureReason.LockedOut
                => "Your account is locked.",

            _ => "Login failed."
        };

        Snackbar.Add(message, Severity.Error);
    }

    private static string ResolveErrorMessage(HubErrorCode? errorCode)
    {
        return errorCode switch
        {
            HubErrorCode.InvalidCredentials =>
                "Invalid username or password. Please check your credentials and try again.",

            HubErrorCode.LockedOut =>
                "Your account is temporarily locked due to too many failed login attempts. Please try again later.",

            HubErrorCode.RequiresMfa =>
                "Additional verification is required to complete your sign-in.",

            HubErrorCode.DeviceLimitExceeded =>
                "Device limit reached. You have reached the maximum number of active devices allowed for your account. Please sign out from another device before trying again.",

            HubErrorCode.None =>
                "Unable to complete sign-in. Please try again.",

            _ =>
                "Authentication failed. Please try again or contact support if the problem persists."
        };
    }
}
