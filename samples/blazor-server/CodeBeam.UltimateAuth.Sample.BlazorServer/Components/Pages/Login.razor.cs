using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Components.Pages;

public partial class Login
{
    private string? _username;
    private string? _password;

    private AuthenticationState _authState = null!;

    protected override async Task OnInitializedAsync()
    {
        Diagnostics.Changed += OnDiagnosticsChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await StateManager.EnsureAsync();
            _authState = await AuthStateProvider.GetAuthenticationStateAsync();
            StateHasChanged();
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
        await UAuth.Flows.LoginAsync(request, "/home");
        _authState = await AuthStateProvider.GetAuthenticationStateAsync();
    }

    private async Task ValidateAsync()
    {
        var result = await UAuth.Flows.ValidateAsync();

        Snackbar.Add(
            result.IsValid ? "Session is valid ✅" : $"Session invalid ❌ ({result.State})",
            result.IsValid ? Severity.Success : Severity.Error);
    }

    private async Task LogoutAsync()
    {
        await UAuth.Flows.LogoutAsync();
        Snackbar.Add("Logged out", Severity.Success);
    }

    private async Task RefreshAsync()
    {
        await UAuth.Flows.RefreshAsync();
    }

    private async Task HandleGetMe()
    {
        var profileResult = await UAuth.Users.GetMeAsync();
        if (profileResult.Ok)
        {
            var profile = profileResult.Value;
            Snackbar.Add($"User Profile: {profile?.UserName} ({profile?.DisplayName})", Severity.Info);
        }
        else
        {
            Snackbar.Add($"Failed to get profile: {profileResult.Error}", Severity.Error);
        }
    }

    private async Task ChangeUserInactive()
    {
        ChangeUserStatusAdminRequest request = new ChangeUserStatusAdminRequest
        {
            UserKey = UserKey.FromString("user"),
            NewStatus = UserStatus.Disabled
        };
        var result = await UAuth.Users.ChangeStatusAdminAsync(request);
        if (result.Ok)
        {
            Snackbar.Add($"User is disabled.", Severity.Info);
        }
        else
        {
            Snackbar.Add($"Failed to change user status.", Severity.Error);
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("error", out var error))
            {
                ShowLoginError(error.ToString());
                ClearQueryString();
            }
        }
    }

    private void ShowLoginError(string code)
    {
        var message = code switch
        {
            "invalid" => "Invalid username or password.",
            "locked" => "Your account is locked.",
            "mfa" => "Multi-factor authentication required.",
            _ => "Login failed."
        };

        Snackbar.Add(message, Severity.Error);
    }

    private void ClearQueryString()
    {
        var uri = new Uri(Nav.Uri);
        var clean = uri.GetLeftPart(UriPartial.Path);
        Nav.NavigateTo(clean, replace: true);
    }

    public void Dispose()
    {
        Diagnostics.Changed -= OnDiagnosticsChanged;
    }

}
