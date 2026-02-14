using CodeBeam.UltimateAuth.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Components.Pages;

public partial class Home
{
    private ClaimsPrincipal? _aspNetCoreState;

    [CascadingParameter]
    public UAuthState AuthState { get; set; } = default!;

    [CascadingParameter]
    Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthenticationStateTask;
        _aspNetCoreState = state.User;
    }

    private async Task Logout()
        => await UAuthClient.Flows.LogoutAsync();

    private async Task RefreshSession()
        => await UAuthClient.Flows.RefreshAsync(false);

    private Task CreateUser() => Task.CompletedTask;
    private Task AssignRole() => Task.CompletedTask;
    private Task ChangePassword() => Task.CompletedTask;
}
