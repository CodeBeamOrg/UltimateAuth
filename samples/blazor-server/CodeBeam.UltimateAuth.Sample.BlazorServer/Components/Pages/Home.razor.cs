namespace CodeBeam.UltimateAuth.Sample.BlazorServer.Components.Pages;

public partial class Home
{
    private string? _aspNetUserName;
    private bool _aspNetAuthenticated;
    private bool _uAuthAuthenticated;
    private string? _uAuthUserKey;
    private string? _clientProfile;
    private string? _tenant;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthProvider.GetAuthenticationStateAsync();
        var user = state.User;

        _aspNetAuthenticated = user.Identity?.IsAuthenticated == true;
        _aspNetUserName = user.Identity?.Name;

        var uAuth = StateManager.State;

        _uAuthAuthenticated = uAuth?.IsAuthenticated == true;
        _uAuthUserKey = uAuth?.UserKey?.ToString();
        _tenant = uAuth?.Tenant.Value;
    }

    private async Task Logout()
        => await UAuthClient.Flows.LogoutAsync();

    private async Task RefreshSession()
        => await UAuthClient.Flows.RefreshAsync(false);

    private Task CreateUser() => Task.CompletedTask;
    private Task AssignRole() => Task.CompletedTask;
    private Task ChangePassword() => Task.CompletedTask;
}
