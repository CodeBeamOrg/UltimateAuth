using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Runtime;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Components.Pages;

public partial class Login : UAuthFlowPageBase
{
    private string? _username;
    private string? _password;
    private ClaimsPrincipal? _aspNetCoreState;
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

    [CascadingParameter]
    public UAuthState AuthState { get; set; } = default!;

    [CascadingParameter]
    Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        AuthStateProvider.AuthenticationStateChanged += OnAuthStateChanged;
        Diagnostics.Changed += OnDiagnosticsChanged;
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

            _ => "Login failed."
        };

        Snackbar.Add(message, Severity.Error);
    }

    private void OnAuthStateChanged(Task<AuthenticationState> task)
    {
        _ = HandleAuthStateChangedAsync(task);
    }

    private async Task HandleAuthStateChangedAsync(Task<AuthenticationState> task)
    {
        try
        {
            var state = await task;
            _aspNetCoreState = state.User;
            await InvokeAsync(StateHasChanged);
        }
        catch
        {

        }
    }

    private void OnDiagnosticsChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    private async Task ProgrammaticLogin()
    {
        var deviceId = await DeviceIdProvider.GetOrCreateAsync();
        var request = new LoginRequest
        {
            Identifier = "admin",
            Secret = "admin",
            Device = DeviceContext.FromDeviceId(deviceId),
        };
        await UAuthClient.Flows.LoginAsync(request, "/home");
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

    public override void Dispose()
    {
        base.Dispose();
        AuthStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
        Diagnostics.Changed -= OnDiagnosticsChanged;
        _lockoutCts?.Cancel();
        _lockoutTimer?.Dispose();
    }
}
