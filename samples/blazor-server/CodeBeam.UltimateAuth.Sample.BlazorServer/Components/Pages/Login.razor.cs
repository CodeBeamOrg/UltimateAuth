using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Runtime;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Components.Pages;

public partial class Login
{
    private string? _username;
    private string? _password;
    private ClaimsPrincipal? _aspNetCoreState;
    private UAuthClientProductInfo? _productInfo;
    private MudTextField<string> _usernameField = default!;

    private DateTimeOffset? _lockoutUntil;
    private TimeSpan _remaining;
    private System.Threading.Timer? _countdownTimer;
    private bool _isLocked;
    private DateTimeOffset? _lockoutStartedAt;
    private TimeSpan _lockoutDuration;
    private double _progressPercent;

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

    private bool _shouldFocus;

    protected override void OnParametersSet()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

        _shouldFocus = query["focus"] == "1";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_shouldFocus)
        {
            _shouldFocus = false;
            await _usernameField.FocusAsync();
        }
    }

    private async void OnAuthStateChanged(Task<AuthenticationState> task)
    {
        var state = await task;
        _aspNetCoreState = state.User;
        await InvokeAsync(StateHasChanged);
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

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender)
            return;

        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("error", out var error))
        {
            query.TryGetValue("lockedUntil", out var lockedRaw);
            query.TryGetValue("remainingAttempts", out var remainingRaw);

            HandleLoginError(error.ToString(), lockedRaw.ToString(), remainingRaw.ToString());

            ClearQueryString();
        }
    }

    private void HandleLoginError(string code, string? lockedUntilRaw, string? remainingRaw)
    {
        if (code == "locked" && long.TryParse(lockedUntilRaw, out var unix))
        {
            _lockoutUntil = DateTimeOffset.FromUnixTimeSeconds(unix);
            StartCountdown();
            return;
        }

        ShowLoginError(code, remainingRaw);
    }

    private void ShowLoginError(string code, string? remainingRaw)
    {
        string message;

        switch (code)
        {
            case "invalid_credentials":
                if (int.TryParse(remainingRaw, out var remaining) && remaining > 0)
                {
                    message = $"Invalid username or password. {remaining} attempt(s) remaining.";
                }
                else
                {
                    message = "Invalid username or password.";
                }
                break;

            case "mfa_required":
                message = "Multi-factor authentication required.";
                break;

            default:
                message = "Login failed.";
                break;
        }

        Snackbar.Add(message, Severity.Error);
    }

    private void StartCountdown()
    {
        if (_lockoutUntil is null)
            return;

        _isLocked = true;
        _lockoutStartedAt = DateTimeOffset.UtcNow;
        _lockoutDuration = _lockoutUntil.Value - DateTimeOffset.UtcNow;
        UpdateRemaining();

        _countdownTimer?.Dispose();
        _countdownTimer = new System.Threading.Timer(_ =>
        {
            InvokeAsync(() =>
            {
                UpdateRemaining();

                if (_remaining <= TimeSpan.Zero)
                {
                    _countdownTimer?.Dispose();
                    _isLocked = false;
                    _lockoutUntil = null;
                    _progressPercent = 0;
                }

                StateHasChanged();
            });
        }, null, 0, 1000);
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
            _progressPercent = 0;
            return;
        }

        var elapsed = now - _lockoutStartedAt.Value;

        if (_lockoutDuration.TotalSeconds > 0)
        {
            var percent = 100 - (elapsed.TotalSeconds / _lockoutDuration.TotalSeconds * 100);
            _progressPercent = Math.Max(0, percent);
        }
    }

    private void ClearQueryString()
    {
        var uri = new Uri(Nav.Uri);
        var clean = uri.GetLeftPart(UriPartial.Path);
        Nav.NavigateTo(clean, replace: true);
    }

    public void Dispose()
    {
        AuthStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
        _countdownTimer?.Dispose();
    }
}
