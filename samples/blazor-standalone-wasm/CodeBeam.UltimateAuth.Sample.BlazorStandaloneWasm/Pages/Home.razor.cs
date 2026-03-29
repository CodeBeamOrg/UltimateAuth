using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Common;
using CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Components.Dialogs;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Pages;

public partial class Home : UAuthFlowPageBase
{
    private string _selectedAuthState = "UAuthState";
    private ClaimsPrincipal? _aspNetCoreState;

    private bool _showAdminPreview = false;

    protected override async Task OnInitializedAsync()
    {
        var initial = await AuthStateProvider.GetAuthenticationStateAsync();
        _aspNetCoreState = initial.User;
        AuthStateProvider.AuthenticationStateChanged += OnAuthStateChanged;
        Diagnostics.Changed += OnDiagnosticsChanged;
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

    private async Task Logout() => await UAuthClient.Flows.LogoutAsync();

    private async Task RefreshSession() => await UAuthClient.Flows.RefreshAsync(false);

    private async Task Validate()
    {
        try
        {
            var result = await UAuthClient.Flows.ValidateAsync();

            if (result.IsValid)
            {
                if (result.Snapshot?.Identity.UserStatus == UserStatus.SelfSuspended)
                {
                    Snackbar.Add("Your account is suspended by you.", Severity.Warning);
                    return;
                }
                Snackbar.Add($"Session active • Tenant: {result.Snapshot?.Identity?.Tenant.Value} • User: {result.Snapshot?.Identity?.PrimaryUserName}", Severity.Success);
            }
            else
            {
                switch (result.State)
                {
                    case SessionState.Expired:
                        Snackbar.Add("Session expired. Please sign in again.", Severity.Warning);
                        break;

                    case SessionState.DeviceMismatch:
                        Snackbar.Add("Session invalid for this device.", Severity.Error);
                        break;

                    default:
                        Snackbar.Add($"Session state: {result.State}", Severity.Error);
                        break;
                }
            }
        }
        catch (UAuthTransportException)
        {
            Snackbar.Add("Network error.", Severity.Error);
        }
        catch (UAuthProtocolException)
        {
            Snackbar.Add("Invalid response.", Severity.Error);
        }
        catch (UAuthException ex)
        {
            Snackbar.Add($"UAuth error: {ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Unexpected error: {ex.Message}", Severity.Error);
        }
    }

    private Color GetHealthColor()
    {
        if (Diagnostics.RefreshReauthRequiredCount > 0)
            return Color.Warning;

        if (Diagnostics.TerminatedCount > 0)
            return Color.Error;

        return Color.Success;
    }

    private string GetHealthText()
    {
        if (Diagnostics.RefreshReauthRequiredCount > 0)
            return "Reauthentication Required";

        if (Diagnostics.TerminatedCount > 0)
            return "Session Terminated";

        return "Healthy";
    }

    private string? FormatRelative(DateTimeOffset? utc)
    {
        if (utc is null)
            return null;

        var diff = DateTimeOffset.UtcNow - utc.Value;

        if (diff.TotalSeconds < 5)
            return "just now";

        if (diff.TotalSeconds < 60)
            return $"{(int)diff.Seconds} secs ago";

        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes} min ago";

        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours} hrs ago";

        return utc.Value.ToLocalTime().ToString("dd MMM yyyy");
    }

    private string? FormatLocalTime(DateTimeOffset? utc)
    {
        return utc?.ToLocalTime().ToString("dd MMM yyyy • HH:mm:ss");
    }

    private async Task OpenProfileDialog()
    {
        await DialogService.ShowAsync<ProfileDialog>("Manage Profile", GetDialogParameters(), UAuthDialog.GetDialogOptions());
    }

    private async Task OpenIdentifierDialog()
    {
        await DialogService.ShowAsync<IdentifierDialog>("Manage Identifiers", GetDialogParameters(), UAuthDialog.GetDialogOptions());
    }

    private async Task OpenSessionDialog()
    {
        await DialogService.ShowAsync<SessionDialog>("Manage Sessions", GetDialogParameters(), UAuthDialog.GetDialogOptions());
    }

    private async Task OpenCredentialDialog()
    {
        await DialogService.ShowAsync<CredentialDialog>("Session Diagnostics", GetDialogParameters(), UAuthDialog.GetDialogOptions());
    }

    private async Task OpenAccountStatusDialog()
    {
        await DialogService.ShowAsync<AccountStatusDialog>("Manage Account", GetDialogParameters(), UAuthDialog.GetDialogOptions(MaxWidth.ExtraSmall));
    }

    private async Task OpenUserDialog()
    {
        await DialogService.ShowAsync<UsersDialog>("User Management", GetDialogParameters(), UAuthDialog.GetDialogOptions(MaxWidth.Large));
    }

    private async Task OpenRoleDialog()
    {
        await DialogService.ShowAsync<RoleDialog>("Role Management", GetDialogParameters(), UAuthDialog.GetDialogOptions());
    }

    private async Task OpenResourceApiDialog()
    {
        await DialogService.ShowAsync<ResourceApiDialog>("Resource Api", GetDialogParameters(), UAuthDialog.GetDialogOptions());
    }

    private DialogParameters GetDialogParameters()
    {
        return new DialogParameters
        {
            ["AuthState"] = AuthState
        };
    }

    private async Task SetAccountActiveAsync()
    {
        ChangeUserStatusSelfRequest request = new() { NewStatus = SelfAssignableUserStatus.Active };
        var result = await UAuthClient.Users.ChangeMyStatusAsync(request);

        if (result.IsSuccess)
        {
            Snackbar.Add("Account activated successfully.", Severity.Success);
        }
        else
        {
            Snackbar.Add(result?.Problem?.Detail ?? result?.Problem?.Title ?? "Activation failed.", Severity.Error);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        AuthStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
        Diagnostics.Changed -= OnDiagnosticsChanged;
    }
}
