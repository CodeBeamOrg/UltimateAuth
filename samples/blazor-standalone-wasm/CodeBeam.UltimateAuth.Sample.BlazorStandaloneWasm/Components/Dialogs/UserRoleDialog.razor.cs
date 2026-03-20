using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Components.Dialogs;

public partial class UserRoleDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UAuthState AuthState { get; set; } = default!;

    [Parameter]
    public UserKey UserKey { get; set; } = default!;

    private List<string> _roles = new();
    private List<RoleInfo> _allRoles = new();

    private string? _selectedRole;

    protected override async Task OnInitializedAsync()
    {
        await LoadRoles();
    }

    private async Task LoadRoles()
    {
        var userRoles = await UAuthClient.Authorization.GetUserRolesAsync(UserKey);

        if (userRoles.IsSuccess && userRoles.Value != null)
            _roles = userRoles.Value.Roles.Items.Select(x => x.Name).ToList();

        var roles = await UAuthClient.Authorization.QueryRolesAsync(new RoleQuery
        {
            PageNumber = 1,
            PageSize = 200
        });

        if (roles.IsSuccess && roles.Value != null)
            _allRoles = roles.Value.Items.ToList();
    }

    private async Task AddRole()
    {
        if (string.IsNullOrWhiteSpace(_selectedRole))
            return;

        var result = await UAuthClient.Authorization.AssignRoleToUserAsync(UserKey, _selectedRole);

        if (result.IsSuccess)
        {
            _roles.Add(_selectedRole);
            Snackbar.Add("Role assigned", Severity.Success);
        }
        else
        {
            Snackbar.Add(result.ErrorText ?? "Failed", Severity.Error);
        }

        _selectedRole = null;
    }

    private async Task RemoveRole(string role)
    {
        var confirm = await DialogService.ShowMessageBoxAsync(
            "Remove Role",
            $"Remove {role} from user?",
            yesText: "Remove",
            noText: "Cancel",
            options: new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true, BackgroundClass = "uauth-blur-slight" });

        if (confirm != true)
        {
            Snackbar.Add("Role remove process cancelled.", Severity.Info);
            return;
        }

        if (role == "Admin")
        {
            var confirm2 = await DialogService.ShowMessageBoxAsync(
                "Are You Sure",
                "You are going to remove admin role. This action may cause the application unuseable.",
                yesText: "Remove",
                noText: "Cancel",
                options: new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true, BackgroundClass = "uauth-blur-slight" });

            if (confirm2 != true)
            {
                Snackbar.Add("Role remove process cancelled.", Severity.Info);
                return;
            }
        }

        var result = await UAuthClient.Authorization.RemoveRoleFromUserAsync(UserKey, role);

        if (result.IsSuccess)
        {
            _roles.Remove(role);
            Snackbar.Add("Role removed.", Severity.Success);
        }
        else
        {
            Snackbar.Add(result.ErrorText ?? "Failed", Severity.Error);
        }
    }

    private void Close() => MudDialog.Close();
}
