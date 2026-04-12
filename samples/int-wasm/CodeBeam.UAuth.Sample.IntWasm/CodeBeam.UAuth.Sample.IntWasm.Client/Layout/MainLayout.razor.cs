using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UAuth.Sample.IntWasm.Client.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UAuth.Sample.IntWasm.Client.Layout;

public partial class MainLayout
{
    [CascadingParameter]
    public UAuthState UAuth { get; set; } = default!;

    [CascadingParameter]
    public DarkModeManager DarkModeManager { get; set; } = default!;

    private async Task Refresh()
    {
        await UAuthClient.Flows.RefreshAsync();
    }

    private async Task Logout()
    {
        await UAuthClient.Flows.LogoutAsync();
    }

    private Color GetBadgeColor()
    {
        if (UAuth is null || !UAuth.IsAuthenticated)
            return Color.Error;

        if (UAuth.IsStale)
            return Color.Warning;

        var state = UAuth.Identity?.SessionState;

        if (state is null || state == SessionState.Active)
            return Color.Success;

        if (state == SessionState.Invalid)
            return Color.Error;

        return Color.Warning;
    }

    private void HandleSignInClick()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);

        if (uri.AbsolutePath.EndsWith("/login", StringComparison.OrdinalIgnoreCase))
        {
            Nav.NavigateTo("/login?focus=1", replace: true, forceLoad: true);
            return;
        }

        GoToLoginWithReturn();
    }

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

    private void GoToLoginWithReturn()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);

        if (uri.AbsolutePath.EndsWith("/login", StringComparison.OrdinalIgnoreCase))
        {
            Nav.NavigateTo("/login", replace: true);
            return;
        }

        var current = Nav.ToBaseRelativePath(uri.ToString());
        if (string.IsNullOrWhiteSpace(current))
            current = "home";

        var returnUrl = Uri.EscapeDataString("/" + current.TrimStart('/'));
        Nav.NavigateTo($"/login?returnUrl={returnUrl}", replace: true);
    }
}
