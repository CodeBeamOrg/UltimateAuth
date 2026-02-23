using CodeBeam.UltimateAuth.Client;
using Microsoft.AspNetCore.Components.Authorization;
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

    public override void Dispose()
    {
        base.Dispose();
        AuthStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
        Diagnostics.Changed -= OnDiagnosticsChanged;
    }
}
