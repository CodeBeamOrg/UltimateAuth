using CodeBeam.UltimateAuth.Client;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Components.Pages;

public partial class Home : UAuthFlowPageBase
{
    private string _selectedAuthState = "UAuthState";
    private ClaimsPrincipal? _aspNetCoreState;

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

    private async Task Logout()
        => await UAuthClient.Flows.LogoutAsync();

    private async Task RefreshSession()
        => await UAuthClient.Flows.RefreshAsync(false);

    private Task CreateUser() => Task.CompletedTask;
    private Task AssignRole() => Task.CompletedTask;
    private Task ChangePassword() => Task.CompletedTask;

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

    public override void Dispose()
    {
        base.Dispose();
        AuthStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
        Diagnostics.Changed -= OnDiagnosticsChanged;
    }
}
