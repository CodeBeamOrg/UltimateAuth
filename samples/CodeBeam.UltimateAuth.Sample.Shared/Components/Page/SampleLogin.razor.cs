using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Runtime;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Sample.Shared.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.Shared.Components.Page;

public partial class SampleLogin : UAuthPageBase
{
    private UAuthClientProductInfo? _productInfo;
    private MudTextField<string> _usernameField = default!;

    private CancellationTokenSource? _lockoutCts;
    private PeriodicTimer? _lockoutTimer;
    private DateTimeOffset? _lockoutUntil;
    private TimeSpan _remaining;
    private bool _isLocked;
    private DateTimeOffset? _lockoutStartedAt;
    private TimeSpan _lockoutDuration;
    private double _progressPercent;
    private int? _remainingAttempts = null;

    protected override async Task OnInitializedAsync()
    {
        _productInfo = ClientProductInfoProvider.Get();
    }

    protected override Task OnUAuthPayloadAsync(AuthFlowPayload payload)
    {
        HandleLoginPayload(payload);
        return Task.CompletedTask;
    }

    protected override async Task OnFocusRequestedAsync()
    {
        await _usernameField.FocusAsync();
    }

    private void HandleLoginPayload(AuthFlowPayload payload)
    {
        if (payload.Flow != AuthFlowType.Login)
            return;

        if (payload.Reason == AuthFailureReason.LockedOut && payload.LockoutUntilUtc is { } until)
        {
            _lockoutUntil = until;
            StartCountdown();
        }

        _remainingAttempts = payload.RemainingAttempts;
        ShowLoginError(payload.Reason, payload.RemainingAttempts);
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

            AuthFailureReason.DeviceLimitExceeded
                => "Device limit exceeded. Sign out from another device.",

            _ => "Login failed."
        };

        Snackbar.Add(message, Severity.Error);
    }

    [Inject]
    private IUAuthStateManager StateManager { get; set; } = default!;

    private async Task ProgrammaticLogin()
    {
        var request = new LoginRequest
        {
            Identifier = "admin",
            Secret = "admin"
        };

        var result = await UAuthClient.Flows.LoginAsync(
            request,
            navigate: false,
            returnUrl: ReturnUrl ?? "/home");

        if (!result.IsSuccess)
        {
            Snackbar.Add("Login failed.", Severity.Error);
            return;
        }

        if (!StateManager.State.IsAuthenticated)
        {
            Snackbar.Add(
                "Login succeeded, but authentication state could not be synchronized.",
                Severity.Warning);

            return;
        }

        Navigation.NavigateTo(ReturnUrl ?? "/home");
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

    private void HandleTry(IUAuthTryResult result)
    {
        if (result is TryLoginResult pkce)
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
            }
        }
        else
        {
            Snackbar.Add("Unexpected result type.", Severity.Error);
        }
    }

    private async Task OpenResetDialog()
    {
        await DialogService.ShowAsync<ResetDialog>("Reset Credentials", GetDialogParameters(), GetDialogOptions());
    }

    private DialogOptions GetDialogOptions()
    {
        return new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true
        };
    }

    private DialogParameters GetDialogParameters()
    {
        return new DialogParameters
        {
            ["AuthState"] = AuthState
        };
    }

    public override void Dispose()
    {
        base.Dispose();
        _lockoutCts?.Cancel();
        _lockoutTimer?.Dispose();
    }
}
